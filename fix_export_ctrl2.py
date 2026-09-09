import re

with open('Controllers/ExportacaoController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('public Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)\n        => ExportarAsync(balancoId, "JSON");', 'public Task<ResultadoOperacao<string>> ExportarJsonAsync(int balancoId)\n        => ExportarAsync(balancoId, "JSON");\n\n    public Task<ResultadoOperacao<string>> ExportarPptAsync(int balancoId)\n        => ExportarAsync(balancoId, "PPT");')

code = code.replace('"JSON"  => await _exportService.ExportarJsonAsync(balanco, empresa),', '"JSON"  => await _exportService.ExportarJsonAsync(balanco, empresa),\n            "PPT"   => await _exportService.ExportarPptAsync(balanco, empresa),')

with open('Controllers/ExportacaoController.cs', 'w', encoding='utf-8') as f:
    f.write(code)
