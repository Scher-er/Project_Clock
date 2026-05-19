using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IGrupoEconomicoDao : IDao<GrupoEconomico>
{
    Task<GrupoEconomico?> BuscarPorNomeAsync(string nome);
}
