using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Implementação do log dual (MongoDB + TXT).
///
/// Decisões de design:
///   - Métodos NUNCA lançam exceção. Se Mongo está fora, ainda tentamos
///     escrever o TXT. Se ambos falham, o app continua funcionando.
///   - O TXT é serializado por dia (logs/2025-05-14.txt) com lock simples
///     pra evitar corrupção em writes concorrentes.
///   - Sempre lê o usuário corrente da ISessaoUsuario — se ninguém estiver
///     autenticado, registra como "SISTEMA".
/// </summary>
public class LogService : ILogService
{
    private readonly ILogDao _logDao;
    private readonly ISessaoUsuario _sessao;
    private readonly DatabaseSettings _settings;
    private static readonly SemaphoreSlim _lockTxt = new(1, 1);

    public string NomeServico => "LogService";

    public LogService(ILogDao logDao, ISessaoUsuario sessao, DatabaseSettings settings)
    {
        _logDao = logDao;
        _sessao = sessao;
        _settings = settings;
    }

    public async Task RegistrarAsync(
        TipoEventoLog tipo, string acao, string descricao,
        string? entidade = null, string? entidadeId = null, string? detalhes = null)
    {
        var log = new LogSistema
        {
            DataHora = DateTime.Now,
            TipoEvento = tipo,
            Usuario = _sessao.UsuarioAtual?.Login ?? "SISTEMA",
            Acao = acao,
            Descricao = descricao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Detalhes = detalhes
        };

        // 1) Mongo (não bloqueia o TXT se falhar)
        try
        {
            await _logDao.InserirAsync(log);
        }
        catch (Exception ex)
        {
            // Sinaliza no próprio TXT que o Mongo falhou
            await EscreverTxtAsync(log, $"[mongo-fail: {ex.Message}]");
            return;
        }

        // 2) TXT
        await EscreverTxtAsync(log);
    }

    public Task RegistrarErroAsync(string acao, Exception ex, string? entidade = null)
        => RegistrarAsync(
            TipoEventoLog.Erro,
            acao,
            ex.Message,
            entidade: entidade,
            detalhes: ex.ToString());

    public async Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia)
    {
        try
        {
            return await _logDao.ListarPorDataAsync(dia);
        }
        catch
        {
            // Se Mongo está fora, retorna lista vazia em vez de quebrar a UI
            return Array.Empty<LogSistema>();
        }
    }

    private async Task EscreverTxtAsync(LogSistema log, string? observacao = null)
    {
        try
        {
            var dir = _settings.CaminhoPastaLogsTxt;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var arquivo = Path.Combine(dir, $"{log.DataHora:yyyy-MM-dd}.txt");
            var linha = FormatarLinha(log, observacao);

            await _lockTxt.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(arquivo, linha + Environment.NewLine);
            }
            finally
            {
                _lockTxt.Release();
            }
        }
        catch
        {
            // Última linha de defesa — se nem o TXT funciona, perdeu o log, mas o app continua
        }
    }

    private static string FormatarLinha(LogSistema log, string? obs)
    {
        // Formato pipe-delimitado, fácil de ler e parsear depois
        // [HH:mm:ss.fff] TIPO | usuario | acao | descricao | entidade(id) | detalhes
        var pedacos = new List<string>
        {
            $"[{log.DataHora:HH:mm:ss.fff}]",
            log.TipoEvento.ToString().ToUpperInvariant().PadRight(11),
            log.Usuario,
            log.Acao,
            log.Descricao
        };

        if (!string.IsNullOrWhiteSpace(log.Entidade))
        {
            pedacos.Add($"{log.Entidade}({log.EntidadeId ?? "-"})");
        }
        if (!string.IsNullOrWhiteSpace(log.Detalhes))
        {
            pedacos.Add(log.Detalhes.Replace("\r", " ").Replace("\n", " "));
        }
        if (!string.IsNullOrWhiteSpace(obs))
        {
            pedacos.Add(obs);
        }
        return string.Join(" | ", pedacos);
    }
}
