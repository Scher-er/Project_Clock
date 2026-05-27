using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IContaPadraoDao : IDao<ContaPadrao>
{
    Task<ContaPadrao?> BuscarPorCodigoAsync(string codigo);

    /// <summary>Lista todas as contas com a hierarquia montada (Filhas preenchidas).</summary>
    Task<IEnumerable<ContaPadrao>> ListarHierarquiaAsync();

    Task<IEnumerable<ContaPadrao>> ListarPorGrupoAsync(GrupoContaPrincipal grupo);
}
