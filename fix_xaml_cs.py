import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Remove ReconstruirTabelaAction logic from constructor
code = re.sub(r'_viewModel\.ReconstruirTabelaAction \+= \(\) => \s*\{\s*AtualizarChips\(\);\s*ReconstruirTabela\(\);\s*\};', '_viewModel.ReconstruirTabelaAction += AtualizarChips;', code)

# Remove ReconstruirTabela calls completely
code = code.replace('ReconstruirTabela();', '')

# Remove massive block of procedural construction
pattern_tabela = r'// \?"\?\?"\?\?"\?\?"\?.*?CONSTRU.*?(?:// \?"\?\?"\?\?"\?.*?IMPORTAA)'
# We will just replace it with the handlers
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
    
    // -------------------------------------------------------------------------
    // IMPORTA'''

code = re.sub(r'// [^\n]*CONSTRU[^\n]*\n.*?(?=(// [^\n]*IMPORT|private void DefinirEmpresaSelecionada))', handlers, code, flags=re.DOTALL)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
