using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller de Empresa. Apenas orquestra — toda a lógica de validação e regras
/// fica no <see cref="IEmpresaService"/>. O Controller existe pra deixar a View
/// independente do Service e respeitar o padrão MVC do trabalho.
/// </summary>
public class EmpresaController : IEmpresaController
{
    private readonly IEmpresaService _service;
    public string NomeRecurso => "Empresa";

    public EmpresaController(IEmpresaService service)
    {
        _service = service;
    }

    public Task<ResultadoOperacao<int>> CriarAsync(Empresa entidade)
        => _service.CadastrarAsync(entidade);

    public Task<ResultadoOperacao<int>> CriarComSetoresAsync(Empresa entidade, IEnumerable<int> setoresIds)
        => _service.CadastrarAsync(entidade, setoresIds);

    public Task<ResultadoOperacao<bool>> AtualizarAsync(Empresa entidade)
        => _service.AtualizarAsync(entidade);

    public Task<ResultadoOperacao<bool>> ExcluirAsync(int id)
        => _service.ExcluirAsync(id);

    public async Task<ResultadoOperacao<Empresa?>> BuscarPorIdAsync(int id)
    {
        var r = await _service.BuscarCompletaAsync(id);
        // Adapta: o service usa Empresa (não-nullable), o IController<T> espera T?
        return r.Sucesso
            ? ResultadoOperacao<Empresa?>.Ok(r.Dados, r.Mensagem)
            : ResultadoOperacao<Empresa?>.Falha(r.Mensagem, r.Erros);
    }

    public Task<ResultadoOperacao<Empresa>> BuscarCompletaAsync(int empresaId)
        => _service.BuscarCompletaAsync(empresaId);

    public Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarAsync()
        => _service.ListarAsync();

    public Task<ResultadoOperacao<IEnumerable<Empresa>>> PesquisarAsync(string termo)
        => _service.PesquisarAsync(termo);
}
