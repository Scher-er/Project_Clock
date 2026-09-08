import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Lines where those removed variables were assigned to
code = re.sub(r'_atualizandoTextoPorSelecao\s*=\s*(true|false);', '', code)
code = re.sub(r'_origemImportacao\s*=\s*".*?";', '', code)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
