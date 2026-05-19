using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Serviço de log. Orquestra duas escritas a cada evento:
///   1) MongoDB (logs estruturados pra query)
///   2) Arquivo TXT diário em logs/YYYY-MM-DD.txt (leitura humana e backup)
/// </summary>
public interface ILogService : IService
{
    /// <summary>Registra um evento. Nunca lança exceção — falhas são engolidas pra não cascatear.</summary>
    Task RegistrarAsync(
        TipoEventoLog tipo,
        string acao,
        string descricao,
        string? entidade = null,
        string? entidadeId = null,
        string? detalhes = null);

    /// <summary>Atalho pra registrar erros — preenche tipo=Erro e detalhes com stack trace.</summary>
    Task RegistrarErroAsync(string acao, Exception ex, string? entidade = null);

    /// <summary>Lista logs de um dia (lê do MongoDB).</summary>
    Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia);
}
