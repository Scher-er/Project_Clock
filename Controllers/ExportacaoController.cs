using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

public class ExportacaoController : IExportacaoController
{
    private readonly IBalancoService _balancoService;
    private readonly IEmpresaService _empresaService;
    private readonly IListagensService _listagens;
    private readonly IExportacaoService _exportService;
    public string NomeRecurso => "Exportacao";

    public ExportacaoController(
        IBalancoService balancoService,
        IEmpresaService empresaService,
        IListagensService listagens,
        IExportacaoService exportService)
    {
        _balancoService = balancoService;
        _empresaService = empresaService;
        _listagens = listagens;
        _exportService = exportService;
    }

    public Task<ResultadoOperacao<string>> ExportarExcelAsync(int balancoId)
        => ExportarAsync(balancoId, "Excel");

    public Task<ResultadoOperacao<string>> ExportarPdfAsync(int balancoId)
        => ExportarAsync(balancoId, "PDF");

    public Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)
        => ExportarAsync(balancoId, "JSON");

    public Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId)
        => ExportarAsync(balancoId, "PPT");

    public string ObterPastaExportacoes() => _exportService.ObterPastaExportacoes();

    public Task<ResultadoOperacao<string>> ImportarJsonAsync(string caminhoArquivo, int usuarioId)
        => _exportService.ImportarJsonAsync(caminhoArquivo, usuarioId);

    /// <summary>
    /// Pipeline comum: carrega balanço completo + empresa completa + plano de contas
    /// → delega ao service.
    /// </summary>
    private async Task<ResultadoOperacao<string>> ExportarAsync(int balancoId, string formato)
    {
        // 1) Balanço completo (com contas + contaPadrao das analíticas)
        var rBalanco = await _balancoService.CarregarAsync(balancoId);
        if (!rBalanco.Sucesso || rBalanco.Dados is null)
            return ResultadoOperacao<string>.Falha(rBalanco.Mensagem);

        var balanco = rBalanco.Dados;

        // 2) Empresa completa (com grupo + setores)
        var rEmpresa = await _empresaService.BuscarCompletaAsync(balanco.EmpresaId);
        if (!rEmpresa.Sucesso || rEmpresa.Dados is null)
            return ResultadoOperacao<string>.Falha(rEmpresa.Mensagem);

        var empresa = rEmpresa.Dados;

        // 3) Plano de contas (necessário pras totalizadoras serem incluídas
        //    e calculadas corretamente no Excel e PDF)
        var planoContas = await _listagens.ListarContasPadraoAsync();

        // 4) Delega ao service de exportação
        return formato switch
        {
            "Excel" => await _exportService.ExportarExcelAsync(balanco, empresa, planoContas),
            "PDF"   => await _exportService.ExportarPdfAsync(balanco, empresa, planoContas),
            "JSON"  => await _exportService.ExportarJsonAsync(balanco, empresa),
            "PPT"   => await _exportService.ExportarPptAsync(balanco, empresa),
            _       => ResultadoOperacao<string>.Falha($"Formato desconhecido: {formato}")
        };
    }
}
