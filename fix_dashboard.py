import re

with open('ViewModels/DashboardViewModel.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('TotalBalancos = lista.Sum(e => e.QuantidadeBalancos);', 'TotalBalancos = lista.Sum(e => e.Balancos?.Count ?? 0);')

with open('ViewModels/DashboardViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(code)

with open('Views/DashboardPage.xaml', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('{Binding QuantidadeBalancos, StringFormat=\'{0} balanços\'}', '{Binding Balancos.Count, StringFormat=\'{0} balanços\'}')

with open('Views/DashboardPage.xaml', 'w', encoding='utf-8') as f:
    f.write(code)
