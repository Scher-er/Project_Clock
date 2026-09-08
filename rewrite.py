import re

def remove_method(code, method_name):
    pattern = r'(private|public|protected|internal)\s+(async\s+)?(void|Task|[\w\<\>\[\]\?]+)\s+' + method_name + r'\s*\('
    match = re.search(pattern, code)
    if not match:
        return code
    
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

with open(r'C:\Users\scher\.gemini\antigravity\brain\be373515-a375-4870-8f2b-4584c0f1edb6\scratch\PlanilhamentoPage.xaml.cs.bak', 'r', encoding='utf-8') as f:
    code = f.read()

new_class_def = """using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;
using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class PlanilhamentoPage : ContentPage
{
    private readonly PlanilhamentoViewModel _viewModel;
    private readonly IPlanilhamentoController _controller;
    private readonly ISessaoUsuario _sessao;

    private string? _hashPdfImportado;
    private string? _nomeArquivoImportado;
    private string _origemImportacao = "Manual";
    private byte[]? _ultimoPdfBytes;
    private string? _ultimoPdfNome;

    private static readonly Color _corAzulHeader = Color.FromArgb(#1F3A60);
    private static readonly Color _corVerde = Color.FromArgb(#0E7C66);
    private static readonly Color _corVermelho = Color.FromArgb(#C0392B);

    public PlanilhamentoPage(PlanilhamentoViewModel viewModel, IPlanilhamentoController controller, ISessaoUsuario sessao)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _controller = controller;
        _sessao = sessao;
        BindingContext = _viewModel;

        _viewModel.PlanoCarregado += () => 
        {
            AtualizarChips();
            ReconstruirTabela();
        };
        _viewModel.ReconstruirTabelaAction += () => 
        {
            AtualizarChips();
            ReconstruirTabela();
        };
        _viewModel.AtualizarKpisAction += AtualizarKPIs;
        _viewModel.FecharResultadosAction += () => painelResultadosEmpresa.IsVisible = false;
        _viewModel.MostrarMensagemAction += async (msg) => await DisplayAlert("Aviso", msg, "OK");
        _viewModel.MostrarConfirmacaoAction = async (title, msg, ok, cancel) => await DisplayAlert(title, msg, ok, cancel);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.Linhas.Any())
        {
            await _viewModel.InicializarAsync();
        }
    }"""

code = re.sub(r'public partial class PlanilhamentoPage : ContentPage.*?protected override async void OnAppearing\(\)\s*\{.*?\}', new_class_def, code, flags=re.DOTALL)

code = code.replace('_periodos', '_viewModel.Periodos')
code = code.replace('_linhas', '_viewModel.Linhas')
code = code.replace('_empresaSelecionada', '_viewModel.EmpresaSelecionada')
code = code.replace('_dresImportadas', '_viewModel.DresImportadas')
code = code.replace('_planoContas', '_viewModel.PlanoContas')

methods_to_remove = [
    'CarregarEmpresasAsync',
    'InicializarPlanoEPeriodoAsync',
    'OnBuscaEmpresaChanged',
    'OnBuscaEmpresaFocused',
    'OnEmpresaResultadoSelecionado',
    'OnAdicionarPeriodoClicado',
    'OnConfirmarAdicionarPeriodo',
    'OnCancelarAdicionarPeriodo',
    'RemoverPeriodo',
    'AtualizarSubtitulo',
    'OnLimparClicado',
    'OnSalvarClicado'
]

for method in methods_to_remove:
    code = remove_method(code, method)

with open('Views/PlanilhamentoPage.xaml.cs', 'w', encoding='utf-8') as f:
    f.write(code)
