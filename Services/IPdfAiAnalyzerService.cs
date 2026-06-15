using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Análise de PDF de balanço usando IA generativa.
///
/// Diferente do <see cref="IPdfParserService"/> (heurístico, regex), este envia
/// o PDF inteiro pro modelo que vê tanto o texto quanto o layout/imagens da
/// página. Funciona bem com PDFs de formatos não-padronizados (contadores
/// locais, empresas privadas, modelos antigos).
///
/// Implementação atual: <see cref="GeminiPdfAnalyzerService"/> (Google Gemini API).
/// Interface mantida genérica caso seja necessário trocar de provedor.
/// </summary>
public interface IPdfAiAnalyzerService : IService
{
    /// <summary>
    /// Envia o PDF pra IA e retorna os dados extraídos no mesmo formato
    /// do parser heurístico.
    /// </summary>
    Task<ResultadoOperacao<ImportacaoPdfResultado>> AnalisarAsync(
        Stream pdfStream, string nomeArquivo);

    /// <summary>
    /// Análise AUTOMÁTICA completa: extrai dados da empresa + todos os períodos
    /// + tipo (individual/consolidado) de cada um. Usado pra importação sem
    /// intervenção manual ("cole o PDF e a IA faz tudo").
    /// </summary>
    Task<ResultadoOperacao<AnaliseAutomaticaResultado>> AnalisarAutomaticoAsync(
        Stream pdfStream, string nomeArquivo);

    /// <summary>
    /// Faz um ping na API com um prompt mínimo só pra validar que a key
    /// e o modelo configurados funcionam. Retorna mensagem amigável.
    /// </summary>
    Task<ResultadoOperacao<string>> TestarConexaoAsync();

    /// <summary>True se as configurações necessárias estão presentes.</summary>
    /// <summary>Gera um parecer de crédito textual via IA a partir dos indicadores já calculados.</summary>
    Task<ResultadoOperacao<string>> GerarParecerAsync(AnaliseEmpresa analise);

    Task<bool> ConfiguradoAsync();
}
