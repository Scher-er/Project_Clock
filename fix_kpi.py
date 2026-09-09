import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('''            if (v.HasValue)
            {
                celula.Valor = v.Value;
            }''', '''            if (v.HasValue)
            {
                celula.Valor = v.Value;
                AtualizarKPIs();
            }''')

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
