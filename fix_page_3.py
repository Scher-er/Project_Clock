import re

with open('Views/PlanilhamentoPage.xaml.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# Insert before OnAppearing
code = re.sub(r'protected override async void OnAppearing', '    private void OnBuscaEmpresaFocused(object? sender, FocusEventArgs e)
    {
        _viewModel.BuscaEmpresaFocusedCommand.Execute(null);
    }\n    protected override async void OnAppearing', code)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
