using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

public class ConfiguracaoLocalService : IConfiguracaoLocalService
{
    private readonly IConfiguracaoLocalDao _dao;
    public string NomeServico => "ConfiguracaoLocalService";

    public ConfiguracaoLocalService(IConfiguracaoLocalDao dao)
    {
        _dao = dao;
    }

    public async Task<string> ObterTemaAsync()
    {
        var v = await _dao.ObterValorAsync(ChavesConfiguracao.Tema);
        return string.IsNullOrEmpty(v) ? "claro" : v;
    }

    public Task DefinirTemaAsync(string tema)
        => _dao.DefinirValorAsync(ChavesConfiguracao.Tema, tema);

    public Task<string?> ObterUltimoUsuarioAsync()
        => _dao.ObterValorAsync(ChavesConfiguracao.UltimoUsuario);

    public Task DefinirUltimoUsuarioAsync(string login)
        => _dao.DefinirValorAsync(ChavesConfiguracao.UltimoUsuario, login);

    public Task<string?> ObterUltimoFiltroAsync()
        => _dao.ObterValorAsync(ChavesConfiguracao.UltimoFiltro);

    public Task DefinirUltimoFiltroAsync(string filtro)
        => _dao.DefinirValorAsync(ChavesConfiguracao.UltimoFiltro, filtro);

    public Task<string?> ObterUltimaPesquisaAsync()
        => _dao.ObterValorAsync(ChavesConfiguracao.UltimaPesquisa);

    public Task DefinirUltimaPesquisaAsync(string termo)
        => _dao.DefinirValorAsync(ChavesConfiguracao.UltimaPesquisa, termo);
}
