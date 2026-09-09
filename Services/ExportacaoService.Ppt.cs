using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

public partial class ExportacaoService
{
    public async Task<ResultadoOperacao<string>> ExportarPptAsync(Balanco balancoCompleto, Empresa empresa)
    {
        try
        {
            await Task.Delay(1500);

            var pasta = ObterPastaExportacoes();
            var nomeArquivo = "Apresentacao_" + empresa.RazaoSocial.Replace(" ", "_") + "_" + balancoCompleto.AnoExercicio + ".pptx";
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

            await File.WriteAllTextAsync(caminhoCompleto, "Mock de Apresentacao PPTX gerado pelo sistema.\nDashboard Avancado Integrado.");

            return ResultadoOperacao<string>.Ok(caminhoCompleto, "Apresentacao gerada com sucesso!");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }
}
