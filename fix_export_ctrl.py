import re

with open('Views/DetalheBalancoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('"Exportar balanA o como:", "Cancelar", null, "PDF", "Excel", "JSON");', '"Exportar balanço como:", "Cancelar", null, "PDF", "Excel", "JSON", "Apresentação PPT (Mock)");')
code = code.replace('"JSON" => await _exportacao.ExportarJsonAsync(_balancoId),\n                _ => ResultadoOperacao<string>.Falha("Formato invAlido.")', '"JSON" => await _exportacao.ExportarJsonAsync(_balancoId),\n                "Apresentação PPT (Mock)" => await _exportacao.ExportarPptAsync(_balancoId),\n                _ => ResultadoOperacao<string>.Falha("Formato inválido.")')

with open('Views/DetalheBalancoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Controllers/IExportacaoController.cs', 'r', encoding='utf-8') as f:
    ctrl = f.read()

ctrl = ctrl.replace('Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId);', 'Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId);\n    Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId);')

with open('Controllers/IExportacaoController.cs', 'w', encoding='utf-8') as f:
    f.write(ctrl)

with open('Controllers/ExportacaoController.cs', 'r', encoding='utf-8') as f:
    ctrl_impl = f.read()

ctrl_impl = ctrl_impl.replace('public async Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)', 'public async Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId)\n    {\n        try\n        {\n            var bal = await _balancoService.BuscarCompletoPorIdAsync(balancoId);\n            if (bal.Dados is null) return ResultadoOperacao<string>.Falha("Balanço não encontrado.");\n            var emp = await _empresaService.BuscarPorIdAsync(bal.Dados.EmpresaId);\n            return await _exportacaoService.ExportarPptAsync(bal.Dados, emp.Dados!);\n        }\n        catch (Exception ex)\n        {\n            return ResultadoOperacao<string>.FalhaExcecao(ex);\n        }\n    }\n\n    public async Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)')

with open('Controllers/ExportacaoController.cs', 'w', encoding='utf-8') as f:
    f.write(ctrl_impl)
