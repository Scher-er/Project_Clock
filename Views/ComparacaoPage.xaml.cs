using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Compara duas empresas lado a lado: indicadores do último balanço de cada e
/// a avaliação de risco. Reaproveita o AnaliseController.
/// </summary>
public partial class ComparacaoPage : ContentPage
{
    private readonly IAnaliseController _controller;
    private static readonly CultureInfo _ptBR = new("pt-BR");
    private static readonly Color _verde = Color.FromArgb("#16A34A");
    private static readonly Color _vermelho = Color.FromArgb("#DC2626");

    public ComparacaoPage(IAnaliseController controller)
    {
        InitializeComponent();
        _controller = controller;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (pkEmpresaA.ItemsSource is null)
            await CarregarEmpresasAsync();
    }

    private async Task CarregarEmpresasAsync()
    {
        var r = await _controller.ListarEmpresasAsync();
        if (r.Sucesso && r.Dados is not null)
        {
            var lista = r.Dados.ToList();
            pkEmpresaA.ItemsSource = lista;
            pkEmpresaB.ItemsSource = lista.ToList();
        }
    }

    private async void OnCompararClicado(object? sender, EventArgs e)
    {
        if (pkEmpresaA.SelectedItem is not Empresa a || pkEmpresaB.SelectedItem is not Empresa b)
        {
            await DisplayAlert("Atenção", "Selecione as duas empresas.", "OK");
            return;
        }
        if (a.Id == b.Id)
        {
            await DisplayAlert("Atenção", "Selecione empresas diferentes.", "OK");
            return;
        }

        layoutComparacao.Children.Clear();
        lblVazio.IsVisible = false;

        var ra = await _controller.AnalisarAsync(a.Id);
        var rb = await _controller.AnalisarAsync(b.Id);

        if (!ra.Sucesso || ra.Dados is null || !rb.Sucesso || rb.Dados is null)
        {
            await DisplayAlert("Erro", "Não foi possível analisar uma das empresas.", "OK");
            return;
        }

        var da = ra.Dados; var db = rb.Dados;
        var indA = da.Indicadores.LastOrDefault();
        var indB = db.Indicadores.LastOrDefault();

        if (indA is null || indB is null)
        {
            await DisplayAlert("Sem dados", "Uma das empresas não tem balanço planilhado.", "OK");
            return;
        }

        layoutComparacao.Children.Add(CardRisco(da, db));
        layoutComparacao.Children.Add(CardComparativo(a.RazaoSocial, b.RazaoSocial, indA, indB));
    }

    private Border CardRisco(AnaliseEmpresa da, AnaliseEmpresa db)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label { Text = "Avaliação de Risco", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        var g = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) },
            ColumnSpacing = 12
        };
        g.Children.Add(BlocoRisco(da, 0));
        g.Children.Add(BlocoRisco(db, 1));
        stack.Children.Add(g);
        return Embrulhar(stack);
    }

    private VerticalStackLayout BlocoRisco(AnaliseEmpresa d, int col)
    {
        var v = new VerticalStackLayout { Spacing = 2 };
        v.Children.Add(new Label { Text = d.Empresa.RazaoSocial, FontAttributes = FontAttributes.Bold, FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation });
        if (d.Risco is AvaliacaoRisco r)
        {
            v.Children.Add(new Label { Text = $"Classe {r.Classe} · {r.NivelRisco}", FontSize = 13, TextColor = r.Classe == "A" || r.Classe == "B" ? _verde : _vermelho, FontAttributes = FontAttributes.Bold });
            v.Children.Add(new Label { Text = $"Score {r.Score}/100", FontSize = 12 });
        }
        else
        {
            v.Children.Add(new Label { Text = "Sem avaliação", FontSize = 12 });
        }
        Grid.SetColumn(v, col);
        return v;
    }

    private Border CardComparativo(string nomeA, string nomeB, IndicadoresBalanco a, IndicadoresBalanco b)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Children.Add(new Label { Text = "Indicadores", FontAttributes = FontAttributes.Bold, FontSize = 15 });

        // Cabeçalho
        stack.Children.Add(LinhaTresColunas("", Curto(nomeA), Curto(nomeB), bold: true));

        void Linha(string nome, decimal? va, decimal? vb, bool pct = false, bool maiorMelhor = true)
        {
            var ta = pct ? Pct(va) : Ind(va);
            var tb = pct ? Pct(vb) : Ind(vb);
            // Destaque de quem está melhor
            Color? ca = null, cb = null;
            if (va is decimal x && vb is decimal y && x != y)
            {
                bool aMelhor = maiorMelhor ? x > y : x < y;
                ca = aMelhor ? _verde : null;
                cb = aMelhor ? null : _verde;
            }
            stack.Children.Add(LinhaTresColunas(nome, ta, tb, corB: cb, corC: ca == _verde ? null : null, corMeio: ca, corDir: cb));
        }

        Linha("Liquidez Corrente", a.LiquidezCorrente, b.LiquidezCorrente);
        Linha("Liquidez Seca", a.LiquidezSeca, b.LiquidezSeca);
        Linha("Liquidez Geral", a.LiquidezGeral, b.LiquidezGeral);
        Linha("Endividamento Geral", a.EndividamentoGeral, b.EndividamentoGeral, pct: true, maiorMelhor: false);
        Linha("Composição do Endiv.", a.ComposicaoEndividamento, b.ComposicaoEndividamento, pct: true, maiorMelhor: false);
        Linha("Imobilização do PL", a.ImobilizacaoPL, b.ImobilizacaoPL, pct: true, maiorMelhor: false);

        if (a.TemDre || b.TemDre)
        {
            stack.Children.Add(new Label { Text = "Rentabilidade", FontAttributes = FontAttributes.Bold, FontSize = 13, Margin = new Thickness(0, 8, 0, 0) });
            Linha("Margem Líquida", a.MargemLiquida, b.MargemLiquida, pct: true);
            Linha("ROE", a.RetornoSobrePL, b.RetornoSobrePL, pct: true);
            Linha("ROA", a.RetornoSobreAtivo, b.RetornoSobreAtivo, pct: true);
        }

        stack.Children.Add(new Label { Text = "Verde = melhor posição no indicador.", FontSize = 10, TextColor = Color.FromArgb("#8A93A2"), Margin = new Thickness(0, 6, 0, 0) });
        return Embrulhar(stack);
    }

    private static Grid LinhaTresColunas(string c1, string c2, string c3, bool bold = false,
        Color? corB = null, Color? corC = null, Color? corMeio = null, Color? corDir = null)
    {
        var g = new Grid { ColumnDefinitions = { new(GridLength.Star), new(110), new(110) }, ColumnSpacing = 8, Padding = new Thickness(0, 3) };
        var attr = bold ? FontAttributes.Bold : FontAttributes.None;
        var l1 = new Label { Text = c1, FontSize = 13, FontAttributes = attr }; Grid.SetColumn(l1, 0);
        var l2 = new Label { Text = c2, FontSize = 13, FontAttributes = bold ? FontAttributes.Bold : (corMeio is not null ? FontAttributes.Bold : FontAttributes.None), HorizontalOptions = LayoutOptions.End, TextColor = corMeio ?? GetDefault() }; Grid.SetColumn(l2, 1);
        var l3 = new Label { Text = c3, FontSize = 13, FontAttributes = bold ? FontAttributes.Bold : (corDir is not null ? FontAttributes.Bold : FontAttributes.None), HorizontalOptions = LayoutOptions.End, TextColor = corDir ?? GetDefault() }; Grid.SetColumn(l3, 2);
        g.Children.Add(l1); g.Children.Add(l2); g.Children.Add(l3);
        return g;
    }

    private static Color GetDefault() =>
        Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#F0F2F5") : Color.FromArgb("#1A1A1A");

    private static Border Embrulhar(View conteudo)
    {
        var card = new Border { Padding = 16, StrokeThickness = 0, Content = conteudo };
        card.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#1B2733"));
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
        return card;
    }

    private static string Ind(decimal? v) => v is decimal x ? x.ToString("N2", _ptBR) : "—";
    private static string Pct(decimal? v) => v is decimal x ? (x * 100m).ToString("N1", _ptBR) + "%" : "—";
    private static string Curto(string s) => s.Length > 16 ? s[..15] + "…" : s;
}
