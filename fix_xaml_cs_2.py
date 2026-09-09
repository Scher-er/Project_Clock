import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Update constructor handlers
code = re.sub(r'_viewModel\.ReconstruirTabelaAction \+= \(\) => \s*\{\s*AtualizarChips\(\);\s*ReconstruirTabela\(\);\s*\};', '_viewModel.ReconstruirTabelaAction += AtualizarChips;', code)

# 2. Remove all ReconstruirTabela(); calls throughout the file
code = code.replace('ReconstruirTabela();', '')

# 3. Add my new handlers right before ReconstruirTabela definition
handlers = '''
    private bool _atualizandoDoUsuario = false;

    private void OnCelulaTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is CelulaPlanilhamento celula)
        {
            _atualizandoDoUsuario = true;
            var v = CelulaPlanilhamento.TentarParsear(e.NewTextValue);
            if (v.HasValue)
            {
                celula.Valor = v.Value;
                AtualizarKPIs();
            }
            _atualizandoDoUsuario = false;
        }
    }

    private void OnCelulaUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry && entry.BindingContext is CelulaPlanilhamento celula)
        {
            if (celula.Valor == 0)
                entry.Text = string.Empty;
            else
                entry.Text = celula.ValorFormatado;
        }
    }

    private async void OnAdicionarContaClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int contaPaiId)
        {
            await AdicionarContaAsync(contaPaiId);
        }
    }
'''
code = re.sub(r'// \?"\?\?"\?\?"\?\?"\?.*?CONSTRU.*?Grid dinAmico\).*?\n', handlers, code, flags=re.DOTALL)

# 4. Remove ReconstruirTabela, AdicionarBotaoNovaConta, AdicionarHeaderCelula definitions
# We need to find the start and end precisely.
pattern_methods_to_remove = r'private void ReconstruirTabela\(\).*?(?=private void AtualizarChips)'
code = re.sub(pattern_methods_to_remove, '', code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
