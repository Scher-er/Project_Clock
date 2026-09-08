import re

with open('ViewModels/AnalisesViewModel.Graficos.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('ObterAnaliseEmpresaAsync', 'AnalisarAsync')
code = code.replace('analise.Balancos', 'analise.Evolucao')
code = code.replace('_ultimaAnalise.Balancos', '_ultimaAnalise.Evolucao')

with open('ViewModels/AnalisesViewModel.Graficos.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('ViewModels/AnalisesViewModel.Ia.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('_ultimaAnalise.Balancos', '_ultimaAnalise.Evolucao')

with open('ViewModels/AnalisesViewModel.Ia.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Views/AnalisesPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

helpers = """
    private string FormatarPercentual(decimal valor) => valor.ToString("P1", new System.Globalization.CultureInfo("pt-BR"));
    private string FormatarIndicador(decimal valor) => valor.ToString("F2", new System.Globalization.CultureInfo("pt-BR"));
}
"""

code = code.replace('}\n', '}\n' + helpers)
# But replace the LAST '}' only.
match = code.rfind('}')
if match != -1:
    code = code[:match] + """
    private string FormatarPercentual(decimal valor) => valor.ToString("P1", new System.Globalization.CultureInfo("pt-BR"));
    private string FormatarIndicador(decimal valor) => valor.ToString("F2", new System.Globalization.CultureInfo("pt-BR"));
}
"""

with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
