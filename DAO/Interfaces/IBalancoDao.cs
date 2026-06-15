using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;

namespace BalancoPatrimonial.App.DAO.Interfaces;

public interface IBalancoDao : IDao<Balanco>
{
    /// <summary>Balanços de uma empresa (shallow, sem contas).</summary>
    Task<IEnumerable<Balanco>> ListarPorEmpresaAsync(int empresaId);

    /// <summary>Balanço com todas as contas preenchidas.</summary>
    Task<Balanco?> BuscarCompletoAsync(int balancoId);

    /// <summary>Existe um balanço pra essa combinação? Útil pra validar duplicidade.</summary>
    Task<bool> ExisteAsync(int empresaId, int anoExercicio, TipoBalanco tipo);

    /// <summary>Id do balanço ativo (ativado=1) com empresa+ano+tipo, ou null.</summary>
    Task<int?> BuscarIdAtivoAsync(int empresaId, int anoExercicio, TipoBalanco tipo);

    /// <summary>Detecta se um PDF já foi importado anteriormente (pelo hash MD5/SHA1).</summary>
    Task<Balanco?> BuscarPorHashPdfAsync(string hash);

    /// <summary>
    /// Insere o balanço + todas as contas em uma única transação.
    /// Garante consistência (não fica "balanço sem contas" se algo falhar).
    /// </summary>
    Task<int> InserirComContasAsync(Balanco balanco);

    /// <summary>Atualiza header + substitui contas (transacional).</summary>
    Task<bool> AtualizarComContasAsync(Balanco balanco);
}
