import re

with open('Services/IExportacaoService.cs', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''    Task<ResultadoOperacao<string>> ExportarJsonAsync(Balanco balancoCompleto, Empresa empresa);

    /// <summary>Exportação Avançada: PowerPoint (.pptx) mockado (Fase 5).</summary>
    Task<ResultadoOperacao<string>> ExportarPptAsync(Balanco balancoCompleto, Empresa empresa);'''

code = code.replace('    Task<ResultadoOperacao<string>> ExportarJsonAsync(Balanco balancoCompleto, Empresa empresa);', replacement)

with open('Services/IExportacaoService.cs', 'w', encoding='utf-8') as f:
    f.write(code)

ppt_cs = '''using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

public partial class ExportacaoService
{
    public async Task<ResultadoOperacao<string>> ExportarPptAsync(Balanco balancoCompleto, Empresa empresa)
    {
        try
        {
            await Task.Delay(1500); // Simulando tempo de geração

            var pasta = ObterPastaExportacoes();
            var nomeArquivo = $"Apresentacao_{empresa.RazaoSocial.Replace(" ", "_")}_{balancoCompleto.Ano}.pptx";
            var caminhoCompleto = Path.Combine(pasta, nomeArquivo);

            // Apenas geramos um arquivo dummy vazio com extensão pptx para fins de mock da Fase 5.
            await File.WriteAllTextAsync(caminhoCompleto, "Mock de Apresentação PPTX gerado pelo sistema.\nDashboard Avançado Integrado.");

            return ResultadoOperacao<string>.Ok(caminhoCompleto, "Apresentação gerada com sucesso!");
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<string>.FalhaExcecao(ex);
        }
    }
}
'''
with open('Services/ExportacaoService.Ppt.cs', 'w', encoding='utf-8') as f:
    f.write(ppt_cs)
