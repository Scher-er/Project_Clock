using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.ViewModels;

public partial class AnalisesViewModel : ObservableObject
{
    private readonly IAnaliseController _controller;

    public ObservableCollection<Empresa> Empresas { get; } = new();

    [ObservableProperty]
    private Empresa? _empresaSelecionada;

    [ObservableProperty]
    private string _subtitulo = "Selecione uma empresa para visualizar a análise gráfica e os KPIs.";

    [ObservableProperty]
    private bool _isPainelVazioVisivel = true;

    [ObservableProperty]
    private string _mensagemVazia = "Nenhuma empresa selecionada.";

    [ObservableProperty]
    private bool _isPainelAnaliseVisivel = false;

    // KPIs
    [ObservableProperty] private string? _kpiAno;
    [ObservableProperty] private string? _kpiAtivo;
    [ObservableProperty] private string? _kpiPassivo;
    [ObservableProperty] private string? _kpiPL;
    [ObservableProperty] private string? _kpiLiquidez;
    [ObservableProperty] private string? _kpiEndividamento;
    [ObservableProperty] private string? _kpiComposicao;
    [ObservableProperty] private string? _kpiImobilizacao;

    // Charts
    [ObservableProperty] private ISeries[] _seriesEvolucao = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxesEvolucao = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _yAxesEvolucao = Array.Empty<Axis>();

    [ObservableProperty] private ISeries[] _seriesCompAtivo = Array.Empty<ISeries>();
    [ObservableProperty] private string _tituloComposicaoAtivo = "Composição do Ativo";

    [ObservableProperty] private ISeries[] _seriesCompPassivo = Array.Empty<ISeries>();
    [ObservableProperty] private string _tituloComposicaoPassivo = "Composição Passivo/PL";

    [ObservableProperty] private ISeries[] _seriesLiquidez = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxesLiquidez = Array.Empty<Axis>();

    // Parecer IA
    [ObservableProperty] private bool _isBuscandoParecer = false;
    [ObservableProperty] private bool _isParecerVisivel = false;
    [ObservableProperty] private string _parecerIa = string.Empty;

    public Action<AnaliseEmpresa>? RenderizarRiscoDetalhesAction { get; set; }

    public AnalisesViewModel(IAnaliseController controller)
    {
        _controller = controller;
    }

    public async Task InicializarAsync()
    {
        var resultado = await _controller.ListarEmpresasAsync();
        if (resultado.Sucesso && resultado.Dados is not null)
        {
            Empresas.Clear();
            foreach (var emp in resultado.Dados.OrderBy(e => e.RazaoSocial))
                Empresas.Add(emp);
        }
    }
}
