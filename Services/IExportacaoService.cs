using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Exportação de balanços patrimoniais nos 3 formatos exigidos pelo trabalho:
///   - Excel (.xlsx) — pra consumo humano e análises adicionais
///   - PDF        — relatório executivo formal pra impressão/email
///   - JSON       — pra integração com outros sistemas do banco
///
/// Todos os arquivos vão pra <c>FileSystem.AppDataDirectory/exportacoes/</c>.
/// O serviço retorna o caminho do arquivo gerado pra que a UI ofereça
/// "abrir" ou "compartilhar".
/// </summary>
public interface IExportacaoService : IService
{
    Task<ResultadoOperacao<string>> ExportarExcelAsync(Balanco balancoCompleto, Empresa empresa, IEnumerable<ContaPadrao> planoContas);
    Task<ResultadoOperacao<string>> ExportarPdfAsync(Balanco balancoCompleto, Empresa empresa, IEnumerable<ContaPadrao> planoContas);
    Task<ResultadoOperacao<string>> ExportarJsonAsync(Balanco balancoCompleto, Empresa empresa);

    /// <summary>Importa um balanço de um arquivo .json para o MySQL (com validação).</summary>
    Task<ResultadoOperacao<string>> ImportarJsonAsync(string caminhoArquivo, int usuarioId);

    /// <summary>Pasta de exportações (cria se não existir).</summary>
    string ObterPastaExportacoes();
}
