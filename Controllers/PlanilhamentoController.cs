using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.DAO.Interfaces;

namespace BalancoPatrimonial.App.Controllers;

public class PlanilhamentoController : IPlanilhamentoController
{
    private readonly IEmpresaService _empresaService;
    private readonly IListagensService _listagens;
    private readonly IBalancoService _balancoService;
    private readonly IPdfParserService _pdfParser;
    private readonly IPdfAiAnalyzerService _aiAnalyzer;
    private readonly IDreDao _dreDao;

    public string NomeRecurso => "Planilhamento";

    private readonly ICvmMockService _cvmMock;

    public PlanilhamentoController(
        IEmpresaService empresaService,
        IListagensService listagens,
        IBalancoService balancoService,
        IPdfParserService pdfParser,
        IPdfAiAnalyzerService aiAnalyzer,
        IDreDao dreDao,
        ICvmMockService cvmMock)
    {
        _cvmMock = cvmMock;
        _empresaService = empresaService;
        _listagens = listagens;
        _balancoService = balancoService;
        _pdfParser = pdfParser;
        _aiAnalyzer = aiAnalyzer;
        _dreDao = dreDao;
    }

    public async Task<ResultadoOperacao<int>> SalvarDreAsync(Dre dre)
    {
        try { return ResultadoOperacao<int>.Ok(await _dreDao.SalvarComSubstituicaoAsync(dre)); }
        catch (Exception ex) { return ResultadoOperacao<int>.FalhaExcecao(ex); }
    }

    public Task<ResultadoOperacao<IEnumerable<Empresa>>> ListarEmpresasAsync()
        => _empresaService.ListarAsync();

    public Task<ResultadoOperacao<ContaPadrao>> CriarContaAnaliticaAsync(int contaPaiId, string descricao)
        => _listagens.CriarContaAnaliticaAsync(contaPaiId, descricao);

    public async Task<ResultadoOperacao<IEnumerable<ContaPadrao>>> ListarPlanoDeContasAsync()
    {
        try
        {
            return ResultadoOperacao<IEnumerable<ContaPadrao>>.Ok(
                await _listagens.ListarContasPadraoAsync());
        }
        catch (Exception ex)
        {
            return ResultadoOperacao<IEnumerable<ContaPadrao>>.FalhaExcecao(ex);
        }
    }

    public Task<ResultadoOperacao<ImportacaoPdfResultado>> ImportarPdfAsync(Stream pdfStream, string nomeArquivo)
        => _pdfParser.AnalisarAsync(pdfStream, nomeArquivo);

    public Task<ResultadoOperacao<ImportacaoPdfResultado>> ImportarPdfComIaAsync(Stream pdfStream, string nomeArquivo)
        => _aiAnalyzer.AnalisarAsync(pdfStream, nomeArquivo);

    public Task<bool> IaConfiguradaAsync()
        => _aiAnalyzer.ConfiguradoAsync();

    public Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ImportarPdfAutomaticoAsync(Stream pdfStream, string nomeArquivo)
        => _aiAnalyzer.AnalisarAutomaticoAsync(pdfStream, nomeArquivo);

    public Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ImportarTickerB3Async(string ticker)
        => _cvmMock.BuscarHistoricoB3Async(ticker);

    public async Task<ResultadoOperacao<Empresa>> BuscarOuCadastrarEmpresaAsync(
        string razaoSocial, string cnpj, Models.Enums.TipoEmpresa tipo, string? uf)
    {
        var cnpjLimpo = new string((cnpj ?? "").Where(char.IsDigit).ToArray());

        // 1) Tenta achar empresa existente pelo CNPJ (ou razão social se CNPJ vazio)
        var listaR = await _empresaService.ListarAsync();
        if (listaR.Sucesso && listaR.Dados is not null)
        {
            var existente = listaR.Dados.FirstOrDefault(e =>
                (!string.IsNullOrEmpty(cnpjLimpo) &&
                 new string((e.Cnpj ?? "").Where(char.IsDigit).ToArray()) == cnpjLimpo)
                || string.Equals(e.RazaoSocial, razaoSocial, StringComparison.OrdinalIgnoreCase));

            if (existente is not null)
                return ResultadoOperacao<Empresa>.Ok(existente, "Empresa já cadastrada.");
        }

        // 2) Não existe — cadastra
        var nova = new Empresa
        {
            RazaoSocial = string.IsNullOrWhiteSpace(razaoSocial) ? "Empresa importada (sem nome)" : razaoSocial,
            Cnpj = cnpjLimpo,
            TipoEmpresa = tipo,
            UfAtuacao = uf,
            Ativo = true
        };

        var cadastroR = await _empresaService.CadastrarAsync(nova);
        if (!cadastroR.Sucesso)
            return ResultadoOperacao<Empresa>.Falha(
                $"Não foi possível cadastrar a empresa automaticamente: {cadastroR.Mensagem}");

        nova.Id = cadastroR.Dados;
        return ResultadoOperacao<Empresa>.Ok(nova, "Empresa cadastrada automaticamente.");
    }

    public BalanceamentoResultado ValidarBalanceamento(Balanco balanco)
        => _balancoService.ValidarBalanceamento(balanco);

    public Task<ResultadoOperacao<int>> SalvarAsync(Balanco balanco, bool substituir = false)
        => _balancoService.SalvarAsync(balanco, substituir);

    public Task<bool> ExisteBalancoAsync(int empresaId, int ano, TipoBalanco tipo)
        => _balancoService.ExisteAsync(empresaId, ano, tipo);
}
