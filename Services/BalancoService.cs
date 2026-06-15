using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

public class BalancoService : IBalancoService
{
    private readonly IBalancoDao _balancoDao;
    private readonly ILogService _log;
    public string NomeServico => "BalancoService";

    public BalancoService(IBalancoDao balancoDao, ILogService log)
    {
        _balancoDao = balancoDao;
        _log = log;
    }

    public async Task<ResultadoOperacao<int>> SalvarAsync(Balanco balanco, bool substituir = false)
    {
        // 1) Validação básica
        var erros = ValidarCampos(balanco);
        if (erros.Count > 0)
            return ResultadoOperacao<int>.Falha("Há erros de validação.", erros);

        // 2) Duplicidade lógica (empresa+ano+tipo entre os ATIVOS)
        if (await _balancoDao.ExisteAsync(balanco.EmpresaId, balanco.AnoExercicio, balanco.TipoBalanco))
        {
            if (!substituir)
            {
                // Sinaliza duplicata pra View poder perguntar "substituir?"
                var dup = ResultadoOperacao<int>.Falha(
                    $"Já existe um balanço {balanco.TipoBalanco} de {balanco.AnoExercicio} para esta empresa.");
                dup.Erros.Add("DUPLICATA");
                return dup;
            }

            // Substituir: marca o balanço ativo existente como inativo (soft delete)
            var idAntigo = await _balancoDao.BuscarIdAtivoAsync(
                balanco.EmpresaId, balanco.AnoExercicio, balanco.TipoBalanco);
            if (idAntigo is int antigo)
            {
                await _balancoDao.ExcluirAsync(antigo); // ativado=0
                await _log.RegistrarAsync(TipoEventoLog.Alteracao, "SUBSTITUIU_BALANCO",
                    $"Balanço id={antigo} substituído (marcado inativo) por nova versão.",
                    "Balanco", antigo.ToString());
            }
        }

        // 3) Duplicidade por hash de PDF (só entre ativos)
        if (!string.IsNullOrEmpty(balanco.HashOrigemPdf))
        {
            var jaImportado = await _balancoDao.BuscarPorHashPdfAsync(balanco.HashOrigemPdf);
            if (jaImportado is not null && !substituir)
            {
                var dup = ResultadoOperacao<int>.Falha(
                    $"Este PDF já foi importado anteriormente (balanço id {jaImportado.Id}).");
                dup.Erros.Add("DUPLICATA");
                return dup;
            }
        }

        // 4) Validação financeira: Ativo = Passivo + PL (apenas aviso, não impede)
        var balanceamento = ValidarBalanceamento(balanco);
        if (!balanceamento.Balanceado)
        {
            await _log.RegistrarAsync(
                TipoEventoLog.Aviso,
                "BALANCO_DESBALANCEADO",
                $"Balanço empresa={balanco.EmpresaId} ano={balanco.AnoExercicio}: {balanceamento.Mensagem}");
            // Note: não retornamos falha — o usuário pode estar planilhando intencionalmente assim.
            // Cabe à View pegar esse aviso (via futura propriedade no resultado) e perguntar.
        }

        // 5) Persistência transacional
        try
        {
            var id = await _balancoDao.InserirComContasAsync(balanco);
            await _log.RegistrarAsync(
                TipoEventoLog.Inclusao,
                "INCLUIU_BALANCO",
                $"Balanço {balanco.TipoBalanco} de {balanco.AnoExercicio} (empresa id={balanco.EmpresaId}) salvo com {balanco.Contas.Count} contas.",
                "Balanco", id.ToString());

            return ResultadoOperacao<int>.Ok(id, "Balanço salvo com sucesso.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("SALVAR_BALANCO", ex, "Balanco");
            return ResultadoOperacao<int>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<Balanco>> CarregarAsync(int balancoId)
    {
        try
        {
            var b = await _balancoDao.BuscarCompletoAsync(balancoId);
            return b is null
                ? ResultadoOperacao<Balanco>.Falha("Balanço não encontrado.")
                : ResultadoOperacao<Balanco>.Ok(b);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("CARREGAR_BALANCO", ex, "Balanco");
            return ResultadoOperacao<Balanco>.FalhaExcecao(ex);
        }
    }

    public Task<bool> ExisteAsync(int empresaId, int anoExercicio, TipoBalanco tipo)
        => _balancoDao.ExisteAsync(empresaId, anoExercicio, tipo);

    public async Task<ResultadoOperacao<bool>> ExcluirAsync(int balancoId)
    {
        try
        {
            var sucesso = await _balancoDao.ExcluirAsync(balancoId);
            if (sucesso)
            {
                await _log.RegistrarAsync(
                    TipoEventoLog.Exclusao,
                    "EXCLUIU_BALANCO",
                    $"Balanço id={balancoId} excluído.",
                    "Balanco", balancoId.ToString());
            }
            return ResultadoOperacao<bool>.Ok(sucesso);
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EXCLUIR_BALANCO", ex, "Balanco");
            return ResultadoOperacao<bool>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<bool>> AtualizarComContasAsync(Balanco balanco)
    {
        var erros = ValidarCampos(balanco);
        if (erros.Count > 0)
            return ResultadoOperacao<bool>.Falha("Há erros de validação.", erros);

        try
        {
            var ok = await _balancoDao.AtualizarComContasAsync(balanco);
            await _log.RegistrarAsync(
                TipoEventoLog.Alteracao,
                "EDITOU_BALANCO",
                $"Balanço id={balanco.Id} editado: {balanco.Contas.Count} contas.",
                "Balanco", balanco.Id.ToString());
            return ResultadoOperacao<bool>.Ok(ok, "Balanço atualizado.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EDITAR_BALANCO", ex, "Balanco");
            return ResultadoOperacao<bool>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<IEnumerable<Balanco>>> ListarPorEmpresaAsync(int empresaId)
    {
        try
        {
            return ResultadoOperacao<IEnumerable<Balanco>>.Ok(
                await _balancoDao.ListarPorEmpresaAsync(empresaId));
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("LISTAR_BALANCOS", ex, "Balanco");
            return ResultadoOperacao<IEnumerable<Balanco>>.FalhaExcecao(ex);
        }
    }

    public BalanceamentoResultado ValidarBalanceamento(Balanco balanco, decimal toleranciaReais = 1.00m)
    {
        // Soma o ativo: todas as contas cujo código começa com "1" (excluindo totalizadoras
        // pra evitar dupla contagem). Conta analítica = não-totalizadora.
        var ativo = balanco.Contas
            .Where(c => c.ContaPadrao is not null
                    && c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.Ativo
                    && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var passivoMaisPL = balanco.Contas
            .Where(c => c.ContaPadrao is not null
                    && (c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.Passivo
                        || c.ContaPadrao.GrupoPrincipal == GrupoContaPrincipal.PatrimonioLiquido)
                    && !c.ContaPadrao.EhTotalizadora)
            .Sum(c => c.Valor);

        var diferenca = ativo - passivoMaisPL;
        var balanceado = Math.Abs(diferenca) <= toleranciaReais;

        var msg = balanceado
            ? "Balanço fecha (Ativo = Passivo + PL)."
            : $"Diferença de R$ {diferenca:N2} entre Ativo (R$ {ativo:N2}) e Passivo+PL (R$ {passivoMaisPL:N2}).";

        return new BalanceamentoResultado(balanceado, ativo, passivoMaisPL, diferenca, msg);
    }

    private static List<string> ValidarCampos(Balanco b)
    {
        var erros = new List<string>();

        if (b.EmpresaId <= 0)
            erros.Add("Empresa é obrigatória.");

        if (b.AnoExercicio < 1900 || b.AnoExercicio > DateTime.Now.Year + 1)
            erros.Add($"Ano de exercício inválido: {b.AnoExercicio}.");

        if (b.UsuarioId <= 0)
            erros.Add("Usuário responsável é obrigatório.");

        if (b.MultiplicadorValores <= 0)
            erros.Add("Multiplicador deve ser positivo.");

        if (b.Contas.Count == 0)
            erros.Add("O balanço precisa ter ao menos uma conta lançada.");

        if (string.IsNullOrWhiteSpace(b.Moeda) || b.Moeda.Length != 3)
            erros.Add("Moeda deve ser um código de 3 letras (BRL, USD).");

        return erros;
    }
}
