using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller das exportações. Recebe um id de balanço, carrega-o completo
/// (com empresa e contas), e delega ao <c>ExportacaoService</c> a geração
/// do arquivo no formato pedido.
/// </summary>
public interface IExportacaoController : IController
{
    Task<ResultadoOperacao<string>> ExportarExcelAsync(int balancoId);
    Task<ResultadoOperacao<string>> ExportarPdfAsync(int balancoId);
    Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId);
    Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId);

    /// <summary>Pasta onde os arquivos exportados ficam (FileSystem.AppDataDirectory/exportacoes).</summary>
    string ObterPastaExportacoes();
    Task<ResultadoOperacao<string>> ImportarJsonAsync(string caminhoArquivo, int usuarioId);
}
