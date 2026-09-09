import re

with open('ViewModels/PlanilhamentoViewModel.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('ReconstruirTabelaAction?.Invoke();', 'AtualizarItensTabela();\n        ReconstruirTabelaAction?.Invoke();')
code = code.replace('PlanoCarregado?.Invoke();', 'AtualizarItensTabela();\n                PlanoCarregado?.Invoke();')

with open('ViewModels/PlanilhamentoViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(code)
