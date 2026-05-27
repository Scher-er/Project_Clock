using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

public interface IBalancoController : IController<Balanco>
{
    Task<ResultadoOperacao<IEnumerable<Balanco>>> ListarPorEmpresaAsync(int empresaId);
    Task<ResultadoOperacao<Balanco>> CarregarCompletoAsync(int balancoId);
    BalanceamentoResultado ValidarBalanceamento(Balanco balanco);
}
