using System.Globalization;
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

    private void MontarRiscoEDetalhes(AnaliseEmpresa dados)
    {
        painelRiscoDetalhes.Children.Clear();
        var ind = dados.Indicadores.LastOrDefault();
        if (ind is null) return;

        if (dados.Risco is AvaliacaoRisco risco)
            painelRiscoDetalhes.Children.Add(CardRisco(risco));

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
            stack.Children.Add(LinhaBullet("+", pf, Color.FromArgb("#16A34A")));
        foreach (var pa in r.PontosAtencao)
            stack.Children.Add(LinhaBullet("-\"", pa, Color.FromArgb("#DC2626")));

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
        stack.Children.Add(new Label { Text = "Outros Indicadores Estruturais", FontAttributes = FontAttributes.Bold, FontSize = 15 });

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

        Linha("Liquidez Imediata", FormatarIndicador(ind.LiquidezImediata), "Disponibilidades / PC");
        Linha("Liquidez Geral", FormatarIndicador(ind.LiquidezGeral), "(AC + RLP) / (PC + PNC)");
        Linha("Capital Circulante Líquido", FormatarCurto(ind.CapitalCirculanteLiquido), "AC - PC");
        Linha("Endividamento Geral", FormatarPercentual(ind.EndividamentoGeral), "Passivo / (Passivo + PL)");
        Linha("Composição do Endividamento", FormatarPercentual(ind.ComposicaoEndividamento), "PC / Passivo Total");

        return EmbrulharCard(stack);
    }

    private Border CardAnaliseVertical(System.Collections.Generic.List<AnaliseVerticalItem> itens)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Análise Vertical (Último balanço)", FontAttributes = FontAttributes.Bold, FontSize = 15 });
        stack.Children.Add(new Label { Text = "Peso de cada grupo no total do Ativo / do Passivo+PL.", FontSize = 11, TextColor = Color.FromArgb("#8A93A2") });

        foreach (var it in itens)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(120), new(70) }, Padding = new Thickness(0, 3), ColumnSpacing = 8 };
            var n = new Label { Text = it.Rotulo, FontSize = 13 }; Grid.SetColumn(n, 0);
            var v = new Label { Text = FormatarCurto(it.Valor), FontSize = 13, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(v, 1);
            var p = new Label { Text = it.PercentualDoTotal is decimal pc ? $"{pc:N1}%" : "-", FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(p, 2);
            g.Children.Add(n); g.Children.Add(v); g.Children.Add(p);
            stack.Children.Add(g);
        }

        return EmbrulharCard(stack);
    }

    private Border CardAnaliseHorizontal(System.Collections.Generic.List<AnaliseHorizontalItem> itens)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Análise Horizontal (Último vs Anterior)", FontAttributes = FontAttributes.Bold, FontSize = 15 });
        stack.Children.Add(new Label { Text = "Variação percentual e absoluta dos principais grupos.", FontSize = 11, TextColor = Color.FromArgb("#8A93A2") });

        foreach (var it in itens)
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(120), new(70) }, Padding = new Thickness(0, 3), ColumnSpacing = 8 };
            var n = new Label { Text = it.Rotulo, FontSize = 13 }; Grid.SetColumn(n, 0);
            var diferenca = it.ValorAtual - it.ValorAnterior;
            var difStr = (diferenca > 0 ? "+" : "") + FormatarCurto(diferenca);
            var v = new Label { Text = difStr, FontSize = 13, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(v, 1);
            
            var pCor = it.VariacaoPercentual > 0 ? Color.FromArgb("#16A34A") : (it.VariacaoPercentual < 0 ? Color.FromArgb("#DC2626") : Colors.Gray);
            var pStr = it.VariacaoPercentual is decimal pc ? $"{pc:N1}%" : "-";
            var p = new Label { Text = pStr, TextColor = pCor, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End }; Grid.SetColumn(p, 2);
            
            g.Children.Add(n); g.Children.Add(v); g.Children.Add(p);
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

    private string FormatarPercentual(decimal? valor) => valor.HasValue ? valor.Value.ToString("P1", new System.Globalization.CultureInfo("pt-BR")) : "-";
    private string FormatarIndicador(decimal? valor) => valor.HasValue ? valor.Value.ToString("F2", new System.Globalization.CultureInfo("pt-BR")) : "-";

    private string FormatarCurto(decimal valor)
    {
        var _cultura = new CultureInfo("pt-BR");
        var v = (double)valor;
        if (Math.Abs(v) >= 1_000_000_000) return $"R$ {v / 1_000_000_000:0.##}B";
        if (Math.Abs(v) >= 1_000_000) return $"R$ {v / 1_000_000:0.##}M";
        if (Math.Abs(v) >= 1_000) return $"R$ {v / 1_000:0.##}k";
        return $"R$ {v:0.##}";
    }
}
