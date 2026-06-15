using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Página de análises visuais. Permite:
///   - Selecionar uma empresa
///   - Ver gráfico de evolução (linhas) com Ativo / Passivo / PL por ano
///   - Ver composição do último balanço (pizzas)
///   - Ver liquidez corrente histórica (barras)
///   - Ver KPIs do balanço mais recente em cards
/// </summary>
public partial class AnalisesPage : ContentPage
{
    private readonly IAnaliseController _controller;
    private readonly CultureInfo _cultura = new("pt-BR");

    private AnaliseEmpresa? _ultimaAnalise;
    private Label? _lblParecerIa;
    private Button? _btnParecerIa;

    // Cores corporativas reutilizadas nos gráficos
    private static readonly SKColor CorAzul = new(0x1F, 0x3A, 0x60);    // Primary
    private static readonly SKColor CorVerde = new(0x16, 0xA3, 0x4A);   // PL / saudável
    private static readonly SKColor CorLaranja = new(0xF5, 0x9E, 0x0B); // Passivo
    private static readonly SKColor CorCinza = new(0x9C, 0xA3, 0xAF);
    private static readonly SKColor CorVermelho = new(0xDC, 0x26, 0x26);

    public AnalisesPage(IAnaliseController controller)
    {
        InitializeComponent();
        _controller = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarEmpresasAsync();
    }

    private async Task CarregarEmpresasAsync()
    {
        var resultado = await _controller.ListarEmpresasAsync();
        if (resultado.Sucesso && resultado.Dados is not null)
        {
            pkEmpresa.ItemsSource = resultado.Dados.ToList();
        }
    }

    private async void OnEmpresaSelecionada(object? sender, EventArgs e)
    {
        if (pkEmpresa.SelectedItem is not Empresa empresa) return;
        await AnalisarAsync(empresa.Id);
    }

    private async Task AnalisarAsync(int empresaId)
    {
        var resultado = await _controller.AnalisarAsync(empresaId);
        if (!resultado.Sucesso || resultado.Dados is null)
        {
            lblMensagemVazia.Text = resultado.Mensagem;
            painelVazio.IsVisible = true;
            painelAnalise.IsVisible = false;
            return;
        }

        var dados = resultado.Dados;
        _ultimaAnalise = dados;

        if (dados.Evolucao.Count == 0)
        {
            lblMensagemVazia.Text = "Esta empresa ainda não tem balanços planilhados.";
            painelVazio.IsVisible = true;
            painelAnalise.IsVisible = false;
            return;
        }

        painelVazio.IsVisible = false;
        painelAnalise.IsVisible = true;
        lblSubtitulo.Text = $"Análise de '{dados.Empresa.RazaoSocial}'.";

        PreencherKpis(dados);
        MontarGraficoEvolucao(dados);
        MontarGraficosComposicao(dados);
        MontarGraficoLiquidez(dados);
        MontarRiscoEDetalhes(dados);
    }

    // ───── KPIs ─────

    private void PreencherKpis(AnaliseEmpresa dados)
    {
        var ultimo = dados.Evolucao.Last();
        var ind = dados.Indicadores.Last();

        lblKpiAno.Text = ultimo.Ano.ToString();
        lblKpiAtivo.Text = FormatarCurto(ultimo.AtivoTotal);
        lblKpiPassivo.Text = FormatarCurto(ultimo.PassivoMaisPL);
        lblKpiPL.Text = FormatarCurto(ultimo.PatrimonioLiquido);

        lblKpiLiquidez.Text = FormatarIndicador(ind.LiquidezCorrente);
        lblKpiLiquidez.TextColor = ColorirIndicadorLiquidez(ind.LiquidezCorrente);

        lblKpiEndividamento.Text = FormatarPercentual(ind.EndividamentoGeral);
        lblKpiEndividamento.TextColor = ColorirIndicadorEndividamento(ind.EndividamentoGeral);

        lblKpiComposicao.Text = FormatarPercentual(ind.ComposicaoEndividamento);
        lblKpiImobilizacao.Text = FormatarPercentual(ind.ImobilizacaoPL);
    }

    // ───── Gráfico de Evolução (linhas) ─────

    private void MontarGraficoEvolucao(AnaliseEmpresa dados)
    {
        var anos = dados.Evolucao.Select(p => p.Ano.ToString()).ToArray();
        var ativos = dados.Evolucao.Select(p => (double)p.AtivoTotal).ToArray();
        var passivos = dados.Evolucao.Select(p => (double)p.PassivoTotal).ToArray();
        var pls = dados.Evolucao.Select(p => (double)p.PatrimonioLiquido).ToArray();

        chartEvolucao.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Ativo Total",
                Values = ativos,
                Stroke = new SolidColorPaint(CorAzul, 3),
                Fill = new SolidColorPaint(CorAzul.WithAlpha(40)),
                GeometryStroke = new SolidColorPaint(CorAzul, 2),
                GeometrySize = 8
            },
            new LineSeries<double>
            {
                Name = "Passivo Total",
                Values = passivos,
                Stroke = new SolidColorPaint(CorLaranja, 3),
                Fill = null,
                GeometryStroke = new SolidColorPaint(CorLaranja, 2),
                GeometrySize = 8
            },
            new LineSeries<double>
            {
                Name = "Patrimônio Líquido",
                Values = pls,
                Stroke = new SolidColorPaint(CorVerde, 3),
                Fill = null,
                GeometryStroke = new SolidColorPaint(CorVerde, 2),
                GeometrySize = 8
            }
        };

        chartEvolucao.XAxes = new[]
        {
            new Axis { Labels = anos, LabelsRotation = 0 }
        };

        chartEvolucao.YAxes = new[]
        {
            new Axis
            {
                Labeler = v => FormatarCurto((decimal)v),
                MinLimit = 0
            }
        };
    }

    // ───── Gráficos de Composição (pizza) ─────

    private void MontarGraficosComposicao(AnaliseEmpresa dados)
    {
        if (dados.ComposicaoAtivo is not null)
        {
            lblTituloComposicaoAtivo.Text = "" + dados.ComposicaoAtivo.Titulo;
            chartCompAtivo.Series = ConstruirPieSeries(dados.ComposicaoAtivo);
        }

        if (dados.ComposicaoPassivoPL is not null)
        {
            lblTituloComposicaoPassivo.Text = "" + dados.ComposicaoPassivoPL.Titulo;
            chartCompPassivoPL.Series = ConstruirPieSeries(dados.ComposicaoPassivoPL);
        }
    }

    private IEnumerable<ISeries> ConstruirPieSeries(ComposicaoBalanco c)
    {
        // Paleta consistente
        var cores = new[] { CorAzul, CorLaranja, CorVerde, CorCinza };

        return c.Fatias.Select((f, i) =>
        {
            var totalGrupo = c.Fatias.Sum(x => x.Valor);
            return new PieSeries<double>
            {
                Name = f.Rotulo,
                Values = new[] { (double)f.Valor },
                Fill = new SolidColorPaint(cores[i % cores.Length]),
                DataLabelsPaint = new SolidColorPaint(SKColors.White)
                {
                    SKTypeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
                },
                DataLabelsSize = 13,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point =>
                {
                    var valor = (decimal)point.Coordinate.PrimaryValue;
                    var pct = totalGrupo > 0 ? (valor / totalGrupo) * 100 : 0;
                    return $"{pct:F1}%";
                }
            };
        });
    }

    // ───── Gráfico de Liquidez (barras) ─────

    private void MontarGraficoLiquidez(AnaliseEmpresa dados)
    {
        var anos = dados.Indicadores.Select(i => i.Ano.ToString()).ToArray();

        double[] Serie(Func<IndicadoresBalanco, decimal?> sel) => dados.Indicadores
            .Select(i => sel(i) is decimal v ? (double)v : double.NaN).ToArray();

        chartLiquidez.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Liquidez Corrente",
                Values = Serie(i => i.LiquidezCorrente),
                Stroke = new SolidColorPaint(CorAzul, 3),
                Fill = null, GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(CorAzul, 3)
            },
            new LineSeries<double>
            {
                Name = "Liquidez Geral",
                Values = Serie(i => i.LiquidezGeral),
                Stroke = new SolidColorPaint(CorVerde, 3),
                Fill = null, GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(CorVerde, 3)
            },
            new LineSeries<double>
            {
                Name = "Endividamento Geral",
                Values = Serie(i => i.EndividamentoGeral),
                Stroke = new SolidColorPaint(CorVermelho, 3),
                Fill = null, GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(CorVermelho, 3)
            }
        };

        chartLiquidez.XAxes = new[] { new Axis { Labels = anos } };
        chartLiquidez.YAxes = new[]
        {
            new Axis { Labeler = v => v.ToString("F2", _cultura), MinLimit = 0 }
        };
        chartLiquidez.LegendPosition = LiveChartsCore.Measure.LegendPosition.Bottom;
    }

    // ───── helpers de formatação ─────

    /// <summary>Formata R$ de forma curta: 1.500.000 → "R$ 1,5M", 250.000 → "R$ 250K".</summary>
    private string FormatarCurto(decimal valor)
    {
        var v = (double)valor;
        return Math.Abs(v) switch
        {
            >= 1_000_000_000 => $"R$ {v / 1_000_000_000:0.##}B",
            >= 1_000_000     => $"R$ {v / 1_000_000:0.##}M",
            >= 1_000         => $"R$ {v / 1_000:0.##}K",
            _                => "R$ " + v.ToString("N2", _cultura)
        };
    }

    private string FormatarIndicador(decimal? v)
        => v.HasValue ? v.Value.ToString("F2", _cultura) : "—";

    private string FormatarPercentual(decimal? v)
        => v.HasValue ? (v.Value * 100).ToString("F1", _cultura) + "%" : "—";

    private static Color ColorirIndicadorLiquidez(decimal? v)
    {
        if (v is null) return Color.FromArgb("#6B7280");
        return v.Value switch
        {
            >= 1.5m => Color.FromArgb("#16A34A"),  // verde — saudável
            >= 1.0m => Color.FromArgb("#F59E0B"),  // amarelo — limite
            _       => Color.FromArgb("#DC2626")   // vermelho — apertado
        };
    }

    private static Color ColorirIndicadorEndividamento(decimal? v)
    {
        if (v is null) return Color.FromArgb("#6B7280");
        return v.Value switch
        {
            <= 0.40m => Color.FromArgb("#16A34A"),  // baixo endividamento
            <= 0.60m => Color.FromArgb("#F59E0B"),  // moderado
            _        => Color.FromArgb("#DC2626")   // alto
        };
    }

    // ───── Avaliação de risco + indicadores detalhados + análise V/H ─────

    private Border CardParecerIa()
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Children.Add(new Label { Text = "Parecer Detalhado (IA)", FontAttributes = FontAttributes.Bold, FontSize = 15 });
        stack.Children.Add(new Label
        {
            Text = "Gera um parecer de crédito redigido pela IA a partir dos indicadores acima. Consome uma requisição da API.",
            FontSize = 11,
            TextColor = Color.FromArgb("#8A93A2")
        });

        _btnParecerIa = new Button
        {
            Text = "Gerar parecer com IA",
            HeightRequest = 42,
            CornerRadius = 8
        };
        _btnParecerIa.SetAppThemeColor(Button.BackgroundColorProperty, Color.FromArgb("#1F3A60"), Color.FromArgb("#2563EB"));
        _btnParecerIa.TextColor = Colors.White;
        _btnParecerIa.Clicked += OnGerarParecerIaClicado;
        stack.Children.Add(_btnParecerIa);

        _lblParecerIa = new Label { Text = "", FontSize = 13, IsVisible = false, LineBreakMode = LineBreakMode.WordWrap };
        stack.Children.Add(_lblParecerIa);

        return EmbrulharCard(stack);
    }

    private async void OnGerarParecerIaClicado(object? sender, EventArgs e)
    {
        if (_ultimaAnalise is null || _btnParecerIa is null || _lblParecerIa is null) return;

        _btnParecerIa.IsEnabled = false;
        _btnParecerIa.Text = "Gerando parecer…";
        _lblParecerIa.IsVisible = false;

        var r = await _controller.GerarParecerIaAsync(_ultimaAnalise);

        if (r.Sucesso && !string.IsNullOrWhiteSpace(r.Dados))
        {
            _lblParecerIa.Text = r.Dados;
            _lblParecerIa.IsVisible = true;
            _btnParecerIa.Text = "Gerar novamente";
        }
        else
        {
            _lblParecerIa.Text = "Não foi possível gerar o parecer pela IA. " + r.Mensagem +
                "\n\n(O parecer automático acima, baseado em regras, continua válido.)";
            _lblParecerIa.IsVisible = true;
            _btnParecerIa.Text = "Tentar novamente";
        }
        _btnParecerIa.IsEnabled = true;
    }

    private void MontarRiscoEDetalhes(AnaliseEmpresa dados)
    {
        painelRiscoDetalhes.Children.Clear();
        var ind = dados.Indicadores.LastOrDefault();
        if (ind is null) return;

        if (dados.Risco is AvaliacaoRisco risco)
            painelRiscoDetalhes.Children.Add(CardRisco(risco));

        painelRiscoDetalhes.Children.Add(CardParecerIa());

        painelRiscoDetalhes.Children.Add(CardIndicadores(ind));

        if (ind.TemDre)
            painelRiscoDetalhes.Children.Add(CardRentabilidade(ind));

        if (dados.AnaliseVertical.Count > 0)
            painelRiscoDetalhes.Children.Add(CardAnaliseVertical(dados.AnaliseVertical));

        if (dados.AnaliseHorizontal.Count > 0)
            painelRiscoDetalhes.Children.Add(CardAnaliseHorizontal(dados.AnaliseHorizontal));
    }

    private static Color CorClasse(string classe) => classe switch
    {
        "A" => Color.FromArgb("#16A34A"),
        "B" => Color.FromArgb("#65A30D"),
        "C" => Color.FromArgb("#F59E0B"),
        _   => Color.FromArgb("#DC2626")
    };

    private Border CardRisco(AvaliacaoRisco r)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Children.Add(new Label { Text = "Avaliação de Risco de Crédito", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        // Score + classe lado a lado
        var topo = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 16 };
        var bolha = new Border
        {
            BackgroundColor = CorClasse(r.Classe),
            StrokeThickness = 0,
            WidthRequest = 64, HeightRequest = 64,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 32 },
            Content = new Label { Text = r.Classe, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        Grid.SetColumn(bolha, 0);
        var infoTopo = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        infoTopo.Children.Add(new Label { Text = $"{r.NivelRisco}", FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = CorClasse(r.Classe) });
        infoTopo.Children.Add(new Label { Text = $"Pontuação: {r.Score}/100", FontSize = 13 });
        Grid.SetColumn(infoTopo, 1);
        topo.Children.Add(bolha); topo.Children.Add(infoTopo);
        stack.Children.Add(topo);

        stack.Children.Add(new Label { Text = r.Parecer, FontSize = 13 });

        foreach (var pf in r.PontosFortes)
            stack.Children.Add(LinhaBullet("✓", pf, Color.FromArgb("#16A34A")));
        foreach (var pa in r.PontosAtencao)
            stack.Children.Add(LinhaBullet("!", pa, Color.FromArgb("#DC2626")));

        return EmbrulharCard(stack);
    }

    private static HorizontalStackLayout LinhaBullet(string marca, string texto, Color cor)
    {
        var h = new HorizontalStackLayout { Spacing = 8 };
        h.Children.Add(new Label { Text = marca, TextColor = cor, FontAttributes = FontAttributes.Bold, WidthRequest = 14 });
        h.Children.Add(new Label { Text = texto, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap });
        return h;
    }

    private Border CardRentabilidade(IndicadoresBalanco ind)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Rentabilidade (a partir da DRE)", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        void Linha(string nome, string valor, string? ajuda = null)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Padding = new Thickness(0, 3) };
            var ns = new VerticalStackLayout { Spacing = 0 };
            ns.Children.Add(new Label { Text = nome, FontSize = 13 });
            if (ajuda is not null) ns.Children.Add(new Label { Text = ajuda, FontSize = 10, TextColor = Color.FromArgb("#8A93A2") });
            Grid.SetColumn(ns, 0);
            var v = new Label { Text = valor, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(v, 1);
            g.Children.Add(ns); g.Children.Add(v);
            stack.Children.Add(g);
        }

        Linha("Margem Bruta", FormatarPercentual(ind.MargemBruta), "Lucro Bruto / Receita");
        Linha("Margem Operacional", FormatarPercentual(ind.MargemOperacional), "EBIT / Receita");
        Linha("Margem Líquida", FormatarPercentual(ind.MargemLiquida), "Lucro Líquido / Receita");
        Linha("ROE (Retorno sobre PL)", FormatarPercentual(ind.RetornoSobrePL), "Lucro Líquido / PL");
        Linha("ROA (Retorno sobre Ativo)", FormatarPercentual(ind.RetornoSobreAtivo), "Lucro Líquido / Ativo");
        Linha("Giro do Ativo", FormatarIndicador(ind.GiroAtivo), "Receita / Ativo");
        Linha("Cobertura de Juros", FormatarIndicador(ind.CoberturaJuros), "EBIT / Desp. Financeiras");

        return EmbrulharCard(stack);
    }

    private Border CardIndicadores(IndicadoresBalanco ind)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Indicadores Detalhados", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        void Linha(string nome, string valor, string? ajuda = null)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) }, Padding = new Thickness(0, 3) };
            var nomeStack = new VerticalStackLayout { Spacing = 0 };
            nomeStack.Children.Add(new Label { Text = nome, FontSize = 13 });
            if (ajuda is not null)
                nomeStack.Children.Add(new Label { Text = ajuda, FontSize = 10, TextColor = Color.FromArgb("#8A93A2") });
            Grid.SetColumn(nomeStack, 0);
            var v = new Label { Text = valor, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(v, 1);
            g.Children.Add(nomeStack); g.Children.Add(v);
            stack.Children.Add(g);
        }

        Linha("Liquidez Corrente", FormatarIndicador(ind.LiquidezCorrente), "AC / PC");
        Linha("Liquidez Seca", FormatarIndicador(ind.LiquidezSeca), "(AC − Estoques) / PC");
        Linha("Liquidez Imediata", FormatarIndicador(ind.LiquidezImediata), "Disponibilidades / PC");
        Linha("Liquidez Geral", FormatarIndicador(ind.LiquidezGeral), "(AC + RLP) / (PC + PNC)");
        Linha("Capital Circulante Líquido", FormatarCurto(ind.CapitalCirculanteLiquido), "AC − PC");
        Linha("Endividamento Geral", FormatarPercentual(ind.EndividamentoGeral), "Passivo / (Passivo + PL)");
        Linha("Composição do Endividamento", FormatarPercentual(ind.ComposicaoEndividamento), "PC / Passivo Total");
        Linha("Participação de Capital de Terceiros", FormatarIndicador(ind.ParticipacaoCapitalTerceiros), "Passivo / PL");
        Linha("Imobilização do PL", FormatarPercentual(ind.ImobilizacaoPL), "ANC / PL");
        Linha("Imobilização dos Recursos Não Correntes", FormatarPercentual(ind.ImobilizacaoRecursosNaoCorrentes), "ANC / (PL + PNC)");

        return EmbrulharCard(stack);
    }

    private Border CardAnaliseVertical(System.Collections.Generic.List<AnaliseVerticalItem> itens)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Análise Vertical (último balanço)", FontAttributes = FontAttributes.Bold, FontSize = 15 });
        stack.Children.Add(new Label { Text = "Peso de cada grupo no total do Ativo / do Passivo+PL.", FontSize = 11, TextColor = Color.FromArgb("#8A93A2") });

        foreach (var it in itens)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(120), new(70) }, Padding = new Thickness(0, 3), ColumnSpacing = 8 };
            var n = new Label { Text = it.Rotulo, FontSize = 13 }; Grid.SetColumn(n, 0);
            var v = new Label { Text = FormatarCurto(it.Valor), FontSize = 13, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(v, 1);
            var p = new Label { Text = it.PercentualDoTotal is decimal pc ? $"{pc:N1}%" : "—", FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(p, 2);
            g.Children.Add(n); g.Children.Add(v); g.Children.Add(p);
            stack.Children.Add(g);
        }
        return EmbrulharCard(stack);
    }

    private Border CardAnaliseHorizontal(System.Collections.Generic.List<AnaliseHorizontalItem> itens)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Análise Horizontal (variação entre os 2 últimos anos)", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        foreach (var it in itens)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(70) }, Padding = new Thickness(0, 3), ColumnSpacing = 8 };
            var n = new Label { Text = it.Rotulo, FontSize = 13 }; Grid.SetColumn(n, 0);
            var varTxt = it.VariacaoPercentual is decimal vp ? $"{(vp >= 0 ? "+" : "")}{vp:N1}%" : "—";
            var cor = it.VariacaoPercentual is decimal v2 ? (v2 >= 0 ? Color.FromArgb("#16A34A") : Color.FromArgb("#DC2626")) : Colors.Gray;
            var p = new Label { Text = varTxt, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = cor, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(p, 1);
            g.Children.Add(n); g.Children.Add(p);
            stack.Children.Add(g);
        }
        return EmbrulharCard(stack);
    }

    private static Border EmbrulharCard(View conteudo)
    {
        var card = new Border { Padding = 16, StrokeThickness = 0, Content = conteudo };
        card.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#1B2733"));
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
        return card;
    }
}
