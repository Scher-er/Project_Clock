using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

public class BalancoController : IBalancoController
{
    private readonly IBalancoService _service;
    public string NomeRecurso => "Balanco";

    public BalancoController(IBalancoService service)
    {
        _service = service;
    }

    public Task<ResultadoOperacao<int>> CriarAsync(Balanco entidade)
        => _service.SalvarAsync(entidade);

    public Task<ResultadoOperacao<bool>> AtualizarAsync(Balanco entidade)
        // Edição de um balanço salvo: substitui o cabeçalho e todas as contas.
        => _service.AtualizarComContasAsync(entidade);

    public Task<ResultadoOperacao<bool>> ExcluirAsync(int id)
        => _service.ExcluirAsync(id);

    public async Task<ResultadoOperacao<Balanco?>> BuscarPorIdAsync(int id)
    {
        var r = await _service.CarregarAsync(id);
        return r.Sucesso
            ? ResultadoOperacao<Balanco?>.Ok(r.Dados, r.Mensagem)
            : ResultadoOperacao<Balanco?>.Falha(r.Mensagem, r.Erros);
    }

    public Task<ResultadoOperacao<Balanco>> CarregarCompletoAsync(int balancoId)
        => _service.CarregarAsync(balancoId);

    public Task<ResultadoOperacao<IEnumerable<Balanco>>> ListarAsync()
        // Não faz sentido listar TODOS os balanços globalmente — sempre é por empresa.
        => Task.FromResult(ResultadoOperacao<IEnumerable<Balanco>>.Falha(
            "Use ListarPorEmpresaAsync — listagem global não é suportada."));

    public Task<ResultadoOperacao<IEnumerable<Balanco>>> ListarPorEmpresaAsync(int empresaId)
        => _service.ListarPorEmpresaAsync(empresaId);

    public BalanceamentoResultado ValidarBalanceamento(Balanco balanco)
        => _service.ValidarBalanceamento(balanco);
}
