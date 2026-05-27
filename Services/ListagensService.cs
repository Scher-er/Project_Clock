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
