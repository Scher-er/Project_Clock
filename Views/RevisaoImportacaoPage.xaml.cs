using System.Globalization;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Mostra, após a importação por IA, a comparação lado a lado entre o que foi
/// EXTRAÍDO (e reconciliado) e o que está IMPRESSO no PDF, por grupo e período.
/// Destaca onde houve ajuste automático, pra o analista validar antes de salvar.
/// </summary>
public partial class RevisaoImportacaoPage : ContentPage
{
    private static readonly CultureInfo _ptBR = new("pt-BR");
    private static readonly Color _verde = Color.FromArgb("#16A34A");
    private static readonly Color _amarelo = Color.FromArgb("#D97706");
    private static readonly Color _vermelho = Color.FromArgb("#DC2626");

    public RevisaoImportacaoPage()
    {
        InitializeComponent();
    }

    public void Inicializar(AnaliseAutomaticaResultado dados)
    {
        var emp = string.IsNullOrWhiteSpace(dados.RazaoSocial) ? "(empresa não identificada)" : dados.RazaoSocial;
        lblSubtitulo.Text = $"{emp} · {dados.Periodos.Count} período(s) detectado(s)";

        layoutRevisao.Children.Clear();

        // Avisos gerais (cache, ajustes, divergências)
        if (dados.Avisos.Count > 0)
            layoutRevisao.Children.Add(CardAvisos(dados.Avisos));

        // Um card por período com a comparação por grupo
        foreach (var p in dados.Periodos)
            layoutRevisao.Children.Add(CardPeriodo(p));
    }

    private Border CardPeriodo(PeriodoDetectado p)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Children.Add(new Label
        {
            Text = $"{p.Label} — {p.ContasMapeadas.Count} contas",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15
        });

        // Cabeçalho da tabela
        var head = new Grid
        {
            ColumnDefinitions = { new(GridLength.Star), new(140), new(140), new(40) },
            ColumnSpacing = 8
        };
        head.Children.Add(ColLabel("Grupo", 0, bold: true));
        head.Children.Add(ColLabel("Extraído", 1, bold: true, end: true));
        head.Children.Add(ColLabel("Impresso (PDF)", 2, bold: true, end: true));
        head.Children.Add(ColLabel("", 3));
        stack.Children.Add(head);

        foreach (var c in p.Comparacoes)
        {
            var g = new Grid
            {
                ColumnDefinitions = { new(GridLength.Star), new(140), new(140), new(40) },
                ColumnSpacing = 8,
                Padding = new Thickness(0, 3)
            };
            g.Children.Add(ColLabel(c.Rotulo, 0));
            g.Children.Add(ColLabel(Fmt(c.Extraido), 1, end: true));
            g.Children.Add(ColLabel(c.Impresso is decimal imp ? Fmt(imp) : "—", 2, end: true));

            // Status: ✓ fecha, ⚠ ajustado, ✗ diverge
            string marca; Color cor;
            if (c.Impresso is null) { marca = "—"; cor = Colors.Gray; }
            else if (c.Fecha && c.Ajuste == 0) { marca = "✓"; cor = _verde; }
            else if (c.Fecha && c.Ajuste != 0) { marca = "⚠"; cor = _amarelo; }
            else { marca = "✗"; cor = _vermelho; }

            var lblStatus = new Label { Text = marca, TextColor = cor, FontAttributes = FontAttributes.Bold, FontSize = 16, HorizontalOptions = LayoutOptions.Center };
            Grid.SetColumn(lblStatus, 3);
            g.Children.Add(lblStatus);
            stack.Children.Add(g);

            // Linha de detalhe do ajuste
            if (c.Ajuste != 0)
            {
                stack.Children.Add(new Label
                {
                    Text = $"   ↳ ajuste automático de {Fmt(c.Ajuste)} lançado para fechar com o PDF (revise as contas deste grupo).",
                    FontSize = 11,
                    TextColor = _amarelo
                });
            }
        }

        // Legenda
        stack.Children.Add(new Label
        {
            Text = "✓ fecha com o PDF   ⚠ fechado via ajuste automático   ✗ diverge   — sem subtotal no PDF",
            FontSize = 10,
            TextColor = Color.FromArgb("#8A93A2"),
            Margin = new Thickness(0, 6, 0, 0)
        });

        return Embrulhar(stack);
    }

    private Border CardAvisos(List<string> avisos)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label { Text = "Observações", FontAttributes = FontAttributes.Bold, FontSize = 15 });
        foreach (var a in avisos)
        {
            var h = new HorizontalStackLayout { Spacing = 8 };
            h.Children.Add(new Label { Text = "•", FontAttributes = FontAttributes.Bold, WidthRequest = 12 });
            h.Children.Add(new Label { Text = a, FontSize = 13, LineBreakMode = LineBreakMode.WordWrap });
            stack.Children.Add(h);
        }
        return Embrulhar(stack);
    }

    private static Label ColLabel(string texto, int col, bool bold = false, bool end = false)
    {
        var l = new Label
        {
            Text = texto,
            FontSize = 13,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            HorizontalOptions = end ? LayoutOptions.End : LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        Grid.SetColumn(l, col);
        return l;
    }

    private static Border Embrulhar(View conteudo)
    {
        var card = new Border { Padding = 16, StrokeThickness = 0, Content = conteudo };
        card.SetAppThemeColor(Border.BackgroundColorProperty, Colors.White, Color.FromArgb("#1B2733"));
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
        return card;
    }

    private static string Fmt(decimal v) => "R$ " + v.ToString("N2", _ptBR);

    private async void OnContinuarClicado(object? sender, EventArgs e)
        => await Navigation.PopAsync();
}
