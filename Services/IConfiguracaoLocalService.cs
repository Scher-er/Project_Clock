using BalancoPatrimonial.App.Interfaces;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Acesso tipado às configurações locais (SQLite).
/// Em vez de chaves soltas, expõe propriedades semânticas que evitam typos.
/// </summary>
public interface IConfiguracaoLocalService : IService
{
    Task<string> ObterTemaAsync();              // "claro" ou "escuro"
    Task DefinirTemaAsync(string tema);

    Task<string?> ObterUltimoUsuarioAsync();
    Task DefinirUltimoUsuarioAsync(string login);

    Task<string?> ObterUltimoFiltroAsync();
    Task DefinirUltimoFiltroAsync(string filtro);

    Task<string?> ObterUltimaPesquisaAsync();
    Task DefinirUltimaPesquisaAsync(string termo);
}
