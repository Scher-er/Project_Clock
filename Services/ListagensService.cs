using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

public class ListagensService : IListagensService
{
    private readonly IGrupoEconomicoDao _grupoDao;
    private readonly ISetorAtividadeDao _setorDao;
    private readonly IContaPadraoDao _contaDao;
    public string NomeServico => "ListagensService";

    public ListagensService(
        IGrupoEconomicoDao grupoDao,
        ISetorAtividadeDao setorDao,
        IContaPadraoDao contaDao)
    {
        _grupoDao = grupoDao;
        _setorDao = setorDao;
        _contaDao = contaDao;
    }

    public async Task<IEnumerable<GrupoEconomico>> ListarGruposEconomicosAsync()
    {
        try { return await _grupoDao.ListarTodosAsync(); }
        catch { return Array.Empty<GrupoEconomico>(); }
    }

    public async Task<IEnumerable<SetorAtividade>> ListarSetoresAsync()
    {
        try { return await _setorDao.ListarTodosAsync(); }
        catch { return Array.Empty<SetorAtividade>(); }
    }

    /// <summary>
    /// Cria uma nova conta analítica como filha de uma totalizadora (subgrupo),
    /// gerando automaticamente um código único na sequência daquele subgrupo.
    ///
    /// Ex: se o pai é "1.01" (Ativo Circulante) e já tem filhas até "1.01.06",
    /// a nova conta recebe "1.01.07". O código segue a área onde foi inserida.
    /// </summary>
    public async Task<ResultadoOperacao<ContaPadrao>> CriarContaAnaliticaAsync(int contaPaiId, string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            return ResultadoOperacao<ContaPadrao>.Falha("Informe a descrição da nova conta.");

        try
        {
            var pai = await _contaDao.BuscarPorIdAsync(contaPaiId);
            if (pai is null)
                return ResultadoOperacao<ContaPadrao>.Falha("Conta-pai não encontrada.");

            var todas = (await _contaDao.ListarTodosAsync()).ToList();
            var irmas = todas.Where(c => c.ContaPaiId == contaPaiId).ToList();

            // Próximo sufixo numérico dentro do subgrupo
            int maxSufixo = 0;
            foreach (var irma in irmas)
            {
                var ultimo = irma.Codigo.Split('.').LastOrDefault();
                if (int.TryParse(ultimo, out var n) && n > maxSufixo)
                    maxSufixo = n;
            }
            var novoCodigo = $"{pai.Codigo}.{(maxSufixo + 1):D2}";

            // Ordem: logo após a última irmã (ou após o pai)
            var ordemBase = irmas.Count > 0 ? irmas.Max(i => i.Ordem) : pai.Ordem;

            var nova = new ContaPadrao
            {
                Codigo = novoCodigo,
                Descricao = descricao.Trim(),
                GrupoPrincipal = pai.GrupoPrincipal,
                ContaPaiId = pai.Id,
                Nivel = pai.Nivel + 1,
                EhTotalizadora = false,
                Ordem = ordemBase + 1,
                Ativa = true
            };

            nova.Id = await _contaDao.InserirAsync(nova);
            return ResultadoOperacao<ContaPadrao>.Ok(nova, $"Conta '{novoCodigo}' criada.");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<ContaPadrao>.FalhaExcecao(ex);
        }
    }

    public async Task<IEnumerable<ContaPadrao>> ListarContasPadraoAsync(GrupoContaPrincipal? grupo = null)
    {
        try
        {
            return grupo.HasValue
                ? await _contaDao.ListarPorGrupoAsync(grupo.Value)
                : await _contaDao.ListarTodosAsync();
        }
        catch
        {
            return Array.Empty<ContaPadrao>();
        }
    }
}
