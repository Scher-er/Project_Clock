using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

public interface IAnaliseController : IController
{
    Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarEmpresasAsync();
    Task<ResultadoOperacao<AnaliseEmpresa>> AnalisarAsync(int empresaId);

    /// <summary>Gera parecer de crédito textual via IA a partir da análise.</summary>
    Task<ResultadoOperacao<string>> GerarParecerIaAsync(AnaliseEmpresa analise);
}

public class AnaliseController : IAnaliseController
{
    private readonly IAnaliseService _analiseService;
    private readonly IEmpresaService _empresaService;
    private readonly IPdfAiAnalyzerService _ia;
    public string NomeRecurso => "Analise";

    public AnaliseController(IAnaliseService analiseService, IEmpresaService empresaService, IPdfAiAnalyzerService ia)
    {
        _analiseService = analiseService;
        _empresaService = empresaService;
        _ia = ia;
    }

    public Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarEmpresasAsync()
        => _empresaService.ListarAsync();

    public Task<ResultadoOperacao<AnaliseEmpresa>> AnalisarAsync(int empresaId)
        => _analiseService.AnalisarAsync(empresaId);

    public Task<ResultadoOperacao<string>> GerarParecerIaAsync(AnaliseEmpresa analise)
        => _ia.GerarParecerAsync(analise);
}
