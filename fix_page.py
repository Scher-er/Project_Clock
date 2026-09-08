import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

code = code.replace('RemoverPeriodo(pRef)', '_viewModel.RemoverPeriodoCommand.Execute(pRef)')
code = code.replace('AtualizarSubtitulo()', '_viewModel.AtualizarSubtitulo()')
code = code.replace('_empresas', '_viewModel.TodasEmpresas')
code = code.replace('private string _origemImportacao = "Manual";', '')
code = code.replace('private bool _atualizandoTextoPorSelecao = false;', '')

# Fix RecarregarPlanoPreservandoValoresAsync inside PlanilhamentoPage.xaml.cs
# We can just delete it, and replace calls to it.
code = re.sub(r'private async Task RecarregarPlanoPreservandoValoresAsync\(\)\s*\{.*?ReconstruirTabela\(\);\s*\}', '', code, flags=re.DOTALL)
code = code.replace('await RecarregarPlanoPreservandoValoresAsync()', 'await _viewModel.RecarregarPlanoPreservandoValoresAsync()')

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
