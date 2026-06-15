using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller da tela de Planilhamento. Orquestra:
///   - Carga inicial: empresas, plano de contas
///   - Importação opcional de PDF (delega ao PdfParserService)
///   - Validação e persistência do balanço (delega ao BalancoService)
/// </summary>
public interface IPlanilhamentoController : IController
{
    Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarEmpresasAsync();
    Task<ResultadoOperacao<IEnumerable<ContaPadrao>>> ListarPlanoDeContasAsync();

    /// <summary>Cria uma nova conta analítica num subgrupo, com código único gerado.</summary>
    Task<ResultadoOperacao<ContaPadrao>> CriarContaAnaliticaAsync(int contaPaiId, string descricao);
    Task<ResultadoOperacao<int>> SalvarDreAsync(Dre dre);

    /// <summary>Importa um PDF e devolve dados pra UI preencher os campos.</summary>
    Task<ResultadoOperacao<ImportacaoPdfResultado>> ImportarPdfAsync(Stream pdfStream, string nomeArquivo);

    /// <summary>
    /// Importa o PDF usando Gemini em vez do parser heurístico.
    /// Funciona bem com PDFs não-padronizados (formato livre, escaneados).
    /// Requer API key configurada na Área de Testes.
    /// </summary>
    Task<ResultadoOperacao<ImportacaoPdfResultado>> ImportarPdfComIaAsync(Stream pdfStream, string nomeArquivo);

    /// <summary>True se as configurações da Gemini API estão presentes.</summary>
    Task<bool> IaConfiguradaAsync();

    /// <summary>
    /// Importação AUTOMÁTICA via Gemini: extrai empresa + todos os períodos + tipos
    /// de uma vez. A UI usa isso pra montar tudo sem intervenção manual.
    /// </summary>
    Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ImportarPdfAutomaticoAsync(Stream pdfStream, string nomeArquivo);

    /// <summary>
    /// Busca uma empresa pelo CNPJ; se não existir, cadastra com os dados fornecidos.
    /// Retorna a empresa (existente ou recém-criada).
    /// </summary>
    Task<ResultadoOperacao<Empresa>> BuscarOuCadastrarEmpresaAsync(
        string razaoSocial, string cnpj, Models.Enums.TipoEmpresa tipo, string? uf);

    /// <summary>Valida o balanceamento (Ativo = Passivo + PL) sem salvar.</summary>
    BalanceamentoResultado ValidarBalanceamento(Balanco balanco);

    /// <summary>Persiste o balanço completo com suas contas (transacional).</summary>
    Task<ResultadoOperacao<int>> SalvarAsync(Balanco balanco, bool substituir = false);
    Task<bool> ExisteBalancoAsync(int empresaId, int ano, TipoBalanco tipo);
}
