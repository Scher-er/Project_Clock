using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IDreDao
{
    /// <summary>Insere ou substitui (soft-delete da anterior) a DRE de empresa+ano+tipo.</summary>
    Task<int> SalvarComSubstituicaoAsync(Dre dre);

    /// <summary>DRE ativa de empresa+ano+tipo, ou null.</summary>
    Task<Dre?> BuscarAtivaAsync(int empresaId, int anoExercicio, TipoBalanco tipo);

    /// <summary>Todas as DREs ativas de uma empresa.</summary>
    Task<IEnumerable<Dre>> ListarPorEmpresaAsync(int empresaId);
}
