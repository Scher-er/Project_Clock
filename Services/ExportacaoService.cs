using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.DAO.Interfaces;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Orquestra as 3 exportações delegando a helpers especializados.
///
/// Mantém o naming dos arquivos consistente:
///   <c>balanco_(razaoSocial)_(ano)_(tipo)_(timestamp).xlsx</c>
/// </summary>
public partial class ExportacaoService : IExportacaoService
{
    private readonly ILogService _log;
    private readonly IEmpresaDao _empresaDao;
    private readonly IBalancoDao _balancoDao;
    private readonly IContaPadraoDao _contaPadraoDao;
    public string NomeServico => "ExportacaoService";

    public ExportacaoService(
        ILogService log,
        IEmpresaDao empresaDao,
        IBalancoDao balancoDao,
        IContaPadraoDao contaPadraoDao)
    {
        _log = log;
        _empresaDao = empresaDao;
        _balancoDao = balancoDao;
        _contaPadraoDao = contaPadraoDao;
    }

    public string ObterPastaExportacoes()
    {
        var pasta = Path.Combine(FileSystem.AppDataDirectory, "exportacoes");
        if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);
        return pasta;
    }

    public async Task<ResultadoOperacao<string>> ExportarExcelAsync(Balanco b, Empresa e, IEnumerable<ContaPadrao> planoContas)
    {
        try
        {
            var caminho = MontarCaminho(e, b, "xlsx");
            GerarExcel(b, e, planoContas.ToList(), caminho);

            await _log.RegistrarAsync(
                TipoEventoLog.Exportacao,
                "EXPORTOU_EXCEL",
                $"Balanço {b.TipoBalanco} {b.AnoExercicio} de '{e.RazaoSocial}' exportado para Excel.",
                "Balanco", b.Id.ToString());

            return ResultadoOperacao<string>.Ok(caminho, "Excel gerado.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EXPORTAR_EXCEL", ex, "Balanco");
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<string>> ExportarPdfAsync(Balanco b, Empresa e, IEnumerable<ContaPadrao> planoContas)
    {
        try
        {
            var caminho = MontarCaminho(e, b, "pdf");
            GerarPdf(b, e, planoContas.ToList(), caminho);

            await _log.RegistrarAsync(
                TipoEventoLog.Exportacao,
                "EXPORTOU_PDF",
                $"Balanço {b.TipoBalanco} {b.AnoExercicio} de '{e.RazaoSocial}' exportado para PDF.",
                "Balanco", b.Id.ToString());

            return ResultadoOperacao<string>.Ok(caminho, "PDF gerado.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EXPORTAR_PDF", ex, "Balanco");
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    public async Task<ResultadoOperacao<string>> ExportarJsonAsync(Balanco b, Empresa e)
    {
        try
        {
            var caminho = MontarCaminho(e, b, "json");
            await GerarJsonAsync(b, e, caminho);

            await _log.RegistrarAsync(
                TipoEventoLog.Exportacao,
                "EXPORTOU_JSON",
                $"Balanço {b.TipoBalanco} {b.AnoExercicio} de '{e.RazaoSocial}' exportado para JSON.",
                "Balanco", b.Id.ToString());

            return ResultadoOperacao<string>.Ok(caminho, "JSON gerado.");
        }
        catch (Exception ex)
        {
            await _log.RegistrarErroAsync("EXPORTAR_JSON", ex, "Balanco");
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }

    /// <summary>
    /// Monta o caminho: pasta/balanco_(slug)_(ano)_(tipo)_(timestamp).(ext).
    /// O slug é a razão social com caracteres especiais removidos.
    /// </summary>
    private string MontarCaminho(Empresa e, Balanco b, string extensao)
    {
        var slug = Slugify(e.RazaoSocial);
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var nome = $"balanco_{slug}_{b.AnoExercicio}_{b.TipoBalanco}_{ts}.{extensao}";
        return Path.Combine(ObterPastaExportacoes(), nome);
    }

    private static string Slugify(string entrada)
    {
        if (string.IsNullOrEmpty(entrada)) return "empresa";
        var sem = new System.Text.StringBuilder();
        foreach (var c in entrada)
        {
            if (char.IsLetterOrDigit(c)) sem.Append(c);
            else if (char.IsWhiteSpace(c)) sem.Append('_');
        }
        var r = sem.ToString().Trim('_').ToLowerInvariant();
        return r.Length > 40 ? r[..40] : r;
    }
}
