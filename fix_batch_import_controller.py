import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

replacement = '''    private async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ProcessarArquivoIaAsync(FileResult arquivo)
    {
        await using var stream = await arquivo.OpenReadAsync();
        using var mem = new MemoryStream();
        await stream.CopyToAsync(mem);
        mem.Position = 0;
        
        return await _controller.ImportarAutomaticoComIaAsync(mem, arquivo.FileName);
    }'''

code = re.sub(r'    private async Task<ResultadoOperacao<AnaliseAutomaticaResultado>> ProcessarArquivoIaAsync\(FileResult arquivo\).*?return await iaService\.AnalisarAutomaticoAsync\(mem, arquivo\.FileName\);\s*\}', replacement, code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
