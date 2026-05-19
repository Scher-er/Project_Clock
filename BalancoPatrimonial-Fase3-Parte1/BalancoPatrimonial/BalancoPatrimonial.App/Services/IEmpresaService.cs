using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Regras de negócio da entidade Empresa. O EmpresaController chama daqui
/// em vez de chamar o DAO direto.
/// </summary>
public interface IEmpresaService : IService
{
    Task<ResultadoOperacao<int>> CadastrarAsync(Empresa empresa, IEnumerable<int>? setoresIds = null);
    Task<ResultadoOperacao<bool>> AtualizarAsync(Empresa empresa);
    Task<ResultadoOperacao<bool>> ExcluirAsync(int empresaId);
    Task<ResultadoOperacao<Empresa>> BuscarCompletaAsync(int empresaId);
    Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarAsync();
    Task<ResultadoOperacao<IEnumerable<Empresa>>> PesquisarAsync(string termo);

    /// <summary>Valida formato do CNPJ (apenas 14 dígitos, sem cálculo de DV).</summary>
    bool ValidarCnpj(string cnpj);

    /// <summary>Remove pontuação do CNPJ.</summary>
    string LimparCnpj(string cnpj);
}
