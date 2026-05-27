using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Service auxiliar para popular dropdowns/pickers na UI.
/// Expõe apenas leituras simples — dados de referência (grupos, setores,
/// contas padrão) que mudam pouco e a UI consulta frequentemente.
///
/// Mantém a View desacoplada dos DAOs específicos.
/// </summary>
public interface IListagensService : IService
{
    Task<IEnumerable<GrupoEconomico>> ListarGruposEconomicosAsync();
    Task<IEnumerable<SetorAtividade>> ListarSetoresAsync();
    Task<IEnumerable<ContaPadrao>> ListarContasPadraoAsync(GrupoContaPrincipal? grupo = null);
}
