import re

with open(r'C:\Users\scher\.gemini\antigravity\brain\be373515-a375-4870-8f2b-4584c0f1edb6\scratch\AnalisesPage.xaml.cs.bak', 'r', encoding='utf-8') as f:
    code = f.read()

match = re.search(r'private void MontarRiscoEDetalhes', code)
tail = code[match.start():]

tail = re.sub(r'\s*painelRiscoDetalhes\.Children\.Add\(CardParecerIa\(\)\);', '', tail)

# Remove CardParecerIa and OnGerarParecerIaClicado if they are in the tail (or code)
# Wait, let's just grab FormatarCurto from original code if needed.
if 'private string FormatarCurto' not in tail:
    fc_match = re.search(r'private string FormatarCurto.*?\}', code, flags=re.DOTALL)
    if fc_match:
        tail += '\n' + fc_match.group(0)
    else:
        # Provide a default
        tail += """
    private string FormatarCurto(decimal valor)
    {
        var _cultura = new CultureInfo("pt-BR");
        if (valor >= 1_000_000_000) return (valor / 1_000_000_000m).ToString("0.##", _cultura) + "B";
        if (valor >= 1_000_000) return (valor / 1_000_000m).ToString("0.##", _cultura) + "M";
        if (valor >= 1_000) return (valor / 1_000m).ToString("0.##", _cultura) + "k";
        return valor.ToString("0", _cultura);
    }
"""

new_code = """using System.Globalization;
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

""" + tail

with open('Views/AnalisesPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(new_code)
