import re

with open('Services/ExportacaoService.Ppt.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('balancoCompleto.Ano', 'balancoCompleto.AnoExercicio')

with open('Services/ExportacaoService.Ppt.cs', 'w', encoding='utf-8') as f:
    f.write(code)
