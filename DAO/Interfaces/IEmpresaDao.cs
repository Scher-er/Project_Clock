using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

/// <summary>
/// Acesso a Empresa. Tem várias formas de carregar — a UI escolhe baseado no
/// que precisa (a tela Empresas usa <see cref="ListarComResumoAsync"/>; a tela
/// de planilhamento usa <see cref="BuscarCompletaAsync"/>).
/// </summary>
public interface IEmpresaDao : IDao<Empresa>
{
    Task<Empresa?> BuscarPorCnpjAsync(string cnpj);

    /// <summary>Empresa + grupo + setores + balanços (todas as navegações).</summary>
    Task<Empresa?> BuscarCompletaAsync(int empresaId);

    /// <summary>
    /// Lista pra exibição na página Empresas — inclui grupo, setores (nomes)
    /// e os anos dos balanços planilhados, mas não as contas dos balanços.
    /// </summary>
    Task<IEnumerable<Empresa>> ListarComResumoAsync();

    /// <summary>Pesquisa por nome, nome fantasia, CNPJ ou grupo econômico.</summary>
    Task<IEnumerable<Empresa>> PesquisarAsync(string termo);

    Task VincularSetorAsync(int empresaId, int setorId, bool principal);
    Task<bool> DesvincularSetorAsync(int empresaId, int setorId);
}
