import re

with open('ViewModels/AnalisesViewModel.Graficos.cs', 'r', encoding='utf-8') as f:
    code = f.read()
code = code.replace('AnoExercicio', 'Ano')
with open('ViewModels/AnalisesViewModel.Graficos.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Views/AnalisesPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('DiferencaAbsoluta', 'Diferenca')

code = code.replace('private string FormatarPercentual(decimal valor) => valor.ToString("P1", new System.Globalization.CultureInfo("pt-BR"));',
                    'private string FormatarPercentual(decimal? valor) => valor.HasValue ? valor.Value.ToString("P1", new System.Globalization.CultureInfo("pt-BR")) : "-";')

code = code.replace('private string FormatarIndicador(decimal valor) => valor.ToString("F2", new System.Globalization.CultureInfo("pt-BR"));',
                    'private string FormatarIndicador(decimal? valor) => valor.HasValue ? valor.Value.ToString("F2", new System.Globalization.CultureInfo("pt-BR")) : "-";')

with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
