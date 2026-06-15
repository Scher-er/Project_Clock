using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Lê um PDF de balanço patrimonial publicado por uma empresa (formato CVM/B3),
/// extrai os metadados (tipo, multiplicador, ano) e tenta casar as linhas com
/// o plano de contas padrão.
///
/// **Não persiste nada.** Apenas analisa e devolve. A View/Controller é quem
/// decide o que fazer com o resultado.
/// </summary>
public interface IPdfParserService : IService
{
    /// <summary>
    /// Analisa o PDF. O <paramref name="nomeArquivo"/> é só pra registro
    /// (o stream é o que efetivamente é lido).
    /// </summary>
    Task<ResultadoOperacao<ImportacaoPdfResultado>> AnalisarAsync(
        Stream pdfStream, string nomeArquivo);
}
