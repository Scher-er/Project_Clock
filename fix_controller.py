import re

with open('Controllers/PlanilhamentoController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Add ICvmMockService to constructor
old_ctor = '''    public PlanilhamentoController(
        IEmpresaService empresaService,
        IListagensService listagens,
        IBalancoService balancoService,
        IPdfParserService pdfParser,
        IPdfAiAnalyzerService aiAnalyzer,
        IDreDao dreDao)
    {'''

new_ctor = '''    private readonly ICvmMockService _cvmMock;

    public PlanilhamentoController(
        IEmpresaService empresaService,
        IListagensService listagens,
        IBalancoService balancoService,
        IPdfParserService pdfParser,
        IPdfAiAnalyzerService aiAnalyzer,
        IDreDao dreDao,
        ICvmMockService cvmMock)
    {
        _cvmMock = cvmMock;'''

code = code.replace(old_ctor, new_ctor)

# Add ImportarTickerB3Async method
method = '''    public Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ImportarTickerB3Async(string ticker)
        => _cvmMock.BuscarHistoricoB3Async(ticker);'''

code = code.replace('        => _aiAnalyzer.AnalisarAutomaticoAsync(pdfStream, nomeArquivo);', '        => _aiAnalyzer.AnalisarAutomaticoAsync(pdfStream, nomeArquivo);\n\n' + method)

with open('Controllers/PlanilhamentoController.cs', 'w', encoding='utf-8') as f:
    f.write(code)
