import re

with open('Views/AnalisesPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Replace class signature and constructor
new_class = """using System.Globalization;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class AnalisesPage : ContentPage
{
    private readonly AnalisesViewModel _viewModel;

    public AnalisesPage(AnalisesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _viewModel.RenderizarRiscoDetalhesAction = MontarRiscoEDetalhes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarAsync();
    }
"""

code = re.sub(r'using System\.Globalization;.*?protected override async void OnAppearing\(\)\s*\{.*?\}', new_class, code, flags=re.DOTALL)

# Delete unwanted methods
methods_to_remove = [
    'CarregarEmpresasAsync',
    'OnEmpresaSelecionada',
    'CarregarAnaliseAsync',
    'AtualizarKpisEGraficos',
    'FormatarCurto',
    'MontarGraficosComposicao',
    'ConstruirPieSeries',
    'CardParecerIa',
    'OnGerarParecerIaClicado'
]

def remove_method(code, method_name):
    pattern = r'(private|public|protected|internal)\s+(async\s+)?(void|Task|[\w\<\>\[\]\?]+)\s+' + method_name + r'\s*\('
    match = re.search(pattern, code)
    if not match: return code
    start_idx = match.start()
    brace_idx = code.find('{', start_idx)
    if brace_idx == -1: return code
    count = 1
    i = brace_idx + 1
    while count > 0 and i < len(code):
        if code[i] == '{': count += 1
        elif code[i] == '}': count -= 1
        i += 1
    return code[:start_idx] + code[i:]

for m in methods_to_remove:
    code = remove_method(code, m)

# Remove the call to CardParecerIa() from MontarRiscoEDetalhes
code = re.sub(r'painelRiscoDetalhes\.Children\.Add\(CardParecerIa\(\)\);', '', code)

with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
