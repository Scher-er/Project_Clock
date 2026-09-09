using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IMapeamentoDeParaDao : IDao<MapeamentoDePara>
{
    Task<MapeamentoDePara?> BuscarMapeamentoAsync(string textoOriginal, int? empresaId);
    Task<IEnumerable<MapeamentoDePara>> ListarGlobalAsync();
    Task<IEnumerable<MapeamentoDePara>> ListarPorEmpresaAsync(int empresaId);
}
