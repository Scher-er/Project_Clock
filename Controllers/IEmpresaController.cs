using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller da página Empresas. Adiciona ao CRUD genérico (<see cref="IController{T}"/>)
/// os métodos específicos que a View precisa: pesquisa e carregamento completo.
/// </summary>
public interface IEmpresaController : IController<Empresa>
{
    /// <summary>
    /// Cria empresa já vinculando setores em uma operação (UI passa lista de IDs marcados).
    /// </summary>
    Task<ResultadoOperacao<int>> CriarComSetoresAsync(Empresa empresa, IEnumerable<int> setoresIds);

    /// <summary>Pesquisa textual no banco — usada pela barra de pesquisa da página.</summary>
    Task<ResultadoOperacao<IEnumerable<Empresa>>> PesquisarAsync(string termo);

    /// <summary>Carrega empresa com grupo + setores + balanços (pra tela de detalhe/edição).</summary>
    Task<ResultadoOperacao<Empresa>> BuscarCompletaAsync(int empresaId);
}
