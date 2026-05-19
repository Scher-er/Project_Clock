using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface ISetorAtividadeDao : IDao<SetorAtividade>
{
    Task<SetorAtividade?> BuscarPorCodigoAsync(string codigo);

    /// <summary>Setores vinculados a uma empresa (via empresa_setor).</summary>
    Task<IEnumerable<SetorAtividade>> ListarPorEmpresaAsync(int empresaId);
}
