import re

with open('scratch/AnalisesPage.xaml.cs.bak', 'r', encoding='utf-8') as f:
    code = f.read()

# We need everything from MontarRiscoEDetalhes down to the end of the file.
# Plus FormatarCurto, FormatarIndicador, FormatarPercentual from the end of the file.
# Wait, let's just find private void MontarRiscoEDetalhes
match = re.search(r'private void MontarRiscoEDetalhes', code)
tail = code[match.start():]

# We also need CorClasse which is inside the tail already.
# We also need FormatarCurto, FormatarIndicador, FormatarPercentual.
# They are already in the tail? Let's check if they are in the tail or before.
# Actually, I can just use a simple approach: build the new file from scratch and append the tail.

# First, remove painelRiscoDetalhes.Children.Add(CardParecerIa()); from tail
tail = re.sub(r'\s*painelRiscoDetalhes\.Children\.Add\(CardParecerIa\(\)\);', '', tail)

# Remove CardParecerIa and OnGerarParecerIaClicado if they are in the tail
tail = re.sub(r'private Border CardParecerIa\(\).*?EmbrulharCard\(stack\);\s*\}', '', tail, flags=re.DOTALL)
tail = re.sub(r'private async void OnGerarParecerIaClicado.*?_btnParecerIa\.IsEnabled = true;\s*\}', '', tail, flags=re.DOTALL)

# Ensure FormatarCurto is in the file.
if 'private string FormatarCurto' not in tail:
    # grab from the whole file
    fc_match = re.search(r'private string FormatarCurto.*?\}', code, flags=re.DOTALL)
    if fc_match:
        tail += '\n' + fc_match.group(0)

# Build the new class
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
