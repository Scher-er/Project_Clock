import re

with open('Views/DetalheBalancoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('"Exportar balanço como:", "Cancelar", null, "PDF", "Excel", "JSON");', '"Exportar balanço como:", "Cancelar", null, "PDF", "Excel", "JSON", "Apresentação PPT (Mock)");')
code = code.replace('"JSON" => await _exportacao.ExportarJsonAsync(_balancoId),\n                _ => ResultadoOperacao<string>.Falha("Formato inválido.")', '"JSON" => await _exportacao.ExportarJsonAsync(_balancoId),\n                "Apresentação PPT (Mock)" => await _exportacao.ExportarPptAsync(_balancoId),\n                _ => ResultadoOperacao<string>.Falha("Formato inválido.")')

with open('Views/DetalheBalancoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Controllers/DetalheBalancoController.cs', 'r', encoding='utf-8') as f:
    ctrl = f.read()

ctrl = ctrl.replace('Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId);', 'Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId);\n    Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId);')
ctrl = ctrl.replace('public async Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)', 'public async Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId)\n    {\n        var bal = await BuscarBalancoCompletoAsync(balancoId);\n        if (bal.Dados is null) return ResultadoOperacao<string>.Falha("Balanço não encontrado.");\n        var emp = await _empresaService.BuscarPorIdAsync(bal.Dados.EmpresaId);\n        return await _exportacaoService.ExportarPptAsync(bal.Dados, emp.Dados!);\n    }\n\n    public async Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)')

with open('Controllers/DetalheBalancoController.cs', 'w', encoding='utf-8') as f:
    f.write(ctrl)
