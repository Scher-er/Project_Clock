import re

with open('Services/ExportacaoService.Ppt.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('"Mock de ApresentaA Ao PPTX gerado pelo sistema.\nDashboard AvanA ado Integrado."', '"Mock de Apresentação PPTX gerado pelo sistema.\\nDashboard Avançado Integrado."')

with open('Services/ExportacaoService.Ppt.cs', 'w', encoding='utf-8') as f:
    f.write(code)
