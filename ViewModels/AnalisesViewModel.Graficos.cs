using System.Globalization;
using BalancoPatrimonial.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.ViewModels;

public partial class AnalisesViewModel
{
    private AnaliseEmpresa? _ultimaAnalise;
    private readonly CultureInfo _cultura = new("pt-BR");

    private static readonly SKColor CorAzul = new(0x1F, 0x3A, 0x60);    // Primary
    private static readonly SKColor CorVerde = new(0x16, 0xA3, 0x4A);   // PL / saudável
    private static readonly SKColor CorLaranja = new(0xF5, 0x9E, 0x0B); // Passivo
    private static readonly SKColor CorCinza = new(0x9C, 0xA3, 0xAF);
    private static readonly SKColor CorVermelho = new(0xDC, 0x26, 0x26);

    partial void OnEmpresaSelecionadaChanged(Empresa? value)
    {
        if (value != null)
        {
            _ = CarregarAnaliseAsync(value);
        }
        else
        {
            IsPainelVazioVisivel = true;
            IsPainelAnaliseVisivel = false;
            MensagemVazia = "Nenhuma empresa selecionada.";
            Subtitulo = "Selecione uma empresa para visualizar a análise gráfica e os KPIs.";
        }
    }

    private async Task CarregarAnaliseAsync(Empresa empresa)
    {
        IsPainelVazioVisivel = true;
        IsPainelAnaliseVisivel = false;
        MensagemVazia = "Carregando análises...";

        var resultado = await _controller.AnalisarAsync(empresa.Id);

        if (!resultado.Sucesso || resultado.Dados is null)
        {
            MensagemVazia = "Erro ao carregar análise:\n" + resultado.Mensagem;
            return;
        }

        _ultimaAnalise = resultado.Dados;

        if (_ultimaAnalise.Evolucao.Count == 0)
        {
            MensagemVazia = "Esta empresa não possui nenhum balanço salvo. Adicione balanços na tela de Planilhamento para ver as análises.";
            Subtitulo = $"Empresa '{empresa.RazaoSocial}' não possui dados.";
            return;
        }

        AtualizarKpisEGraficos(_ultimaAnalise);
        IsPainelVazioVisivel = false;
        IsPainelAnaliseVisivel = true;
        Subtitulo = $"Analisando evolução e saúde financeira de '{empresa.RazaoSocial}' (Último ano: {_ultimaAnalise.Evolucao.Last().Ano}).";
        RenderizarRiscoDetalhesAction?.Invoke(_ultimaAnalise);
        
        IsParecerVisivel = false;
        IsBuscandoParecer = false;
    }

    private void AtualizarKpisEGraficos(AnaliseEmpresa analise)
    {
        var balancos = analise.Evolucao;
        var ultimo = balancos.Last();
        var penultimo = balancos.Count > 1 ? balancos[balancos.Count - 2] : null;

        KpiAno = ultimo.Ano.ToString();
        KpiAtivo = ultimo.AtivoTotal.ToString("C0", _cultura);
        KpiPassivo = ultimo.PassivoTotal.ToString("C0", _cultura);
        KpiPL = ultimo.PatrimonioLiquido.ToString("C0", _cultura);

        // Indicadores (Liquidez Corrente, Endividamento)
        var liqUltimo = (ultimo.PassivoCirculante > 0) ? ultimo.AtivoCirculante / ultimo.PassivoCirculante : 0;
        var endivUltimo = (ultimo.AtivoTotal > 0) ? (ultimo.PassivoCirculante + ultimo.PassivoNaoCirculante) / ultimo.AtivoTotal : 0;
        var compUltimo = ((ultimo.PassivoCirculante + ultimo.PassivoNaoCirculante) > 0)
            ? ultimo.PassivoCirculante / (ultimo.PassivoCirculante + ultimo.PassivoNaoCirculante) : 0;
        var imobUltimo = (ultimo.PatrimonioLiquido > 0) ? ultimo.AtivoNaoCirculante / ultimo.PatrimonioLiquido : 0;

        KpiLiquidez = liqUltimo.ToString("F2", _cultura);
        KpiEndividamento = endivUltimo.ToString("P1", _cultura);
        KpiComposicao = compUltimo.ToString("P1", _cultura);
        KpiImobilizacao = imobUltimo.ToString("P1", _cultura);

        // --- Gráfico de Evolução (Linhas) ---
        var anos = balancos.Select(b => b.Ano.ToString()).ToArray();
        var ativos = balancos.Select(b => (double)b.AtivoTotal).ToArray();
        var passivos = balancos.Select(b => (double)(b.PassivoCirculante + b.PassivoNaoCirculante)).ToArray();
        var pls = balancos.Select(b => (double)b.PatrimonioLiquido).ToArray();

        SeriesEvolucao = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Ativo Total",
                Values = ativos,
                Stroke = new SolidColorPaint(CorAzul) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(CorAzul) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(CorAzul),
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Name = "Passivo (Exigível)",
                Values = passivos,
                Stroke = new SolidColorPaint(CorLaranja) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(CorLaranja) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(CorLaranja),
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Name = "Patrimônio Líquido",
                Values = pls,
                Stroke = new SolidColorPaint(CorVerde) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(CorVerde) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(CorVerde),
                LineSmoothness = 0.5
            }
        };

        XAxesEvolucao = new[] { new Axis { Labels = anos, LabelsRotation = 0 } };
        YAxesEvolucao = new[] { new Axis { Labeler = v => FormatarCurto((decimal)v), MinLimit = 0 } };

        // --- Gráficos de Composição (Pizza) ---
        TituloComposicaoAtivo = $"Composição do Ativo ({ultimo.Ano})";
        SeriesCompAtivo = new ISeries[]
        {
            new PieSeries<double> { Name = "Circulante", Values = new[] { (double)ultimo.AtivoCirculante }, Fill = new SolidColorPaint(CorAzul) },
            new PieSeries<double> { Name = "Não Circulante", Values = new[] { (double)ultimo.AtivoNaoCirculante }, Fill = new SolidColorPaint(CorCinza) }
        };

        TituloComposicaoPassivo = $"Composição Passivo/PL ({ultimo.Ano})";
        SeriesCompPassivo = new ISeries[]
        {
            new PieSeries<double> { Name = "Passivo Circulante", Values = new[] { (double)ultimo.PassivoCirculante }, Fill = new SolidColorPaint(CorLaranja) },
            new PieSeries<double> { Name = "Passivo Ñ. Circulante", Values = new[] { (double)ultimo.PassivoNaoCirculante }, Fill = new SolidColorPaint(SKColors.DarkOrange) },
            new PieSeries<double> { Name = "Patrimônio Líquido", Values = new[] { (double)ultimo.PatrimonioLiquido }, Fill = new SolidColorPaint(CorVerde) }
        };

        // --- Gráfico de Liquidez (Barras) ---
        var liquidezVals = new double[balancos.Count];
        for (int i = 0; i < balancos.Count; i++)
        {
            var b = balancos[i];
            liquidezVals[i] = b.PassivoCirculante > 0 ? (double)(b.AtivoCirculante / b.PassivoCirculante) : 0;
        }

        SeriesLiquidez = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Liq. Corrente",
                Values = liquidezVals,
                Fill = new SolidColorPaint(CorAzul),
                MaxBarWidth = 40
            }
        };
        XAxesLiquidez = new[] { new Axis { Labels = anos } };
    }

    private string FormatarCurto(decimal valor)
    {
        if (valor >= 1_000_000_000) return (valor / 1_000_000_000m).ToString("0.##", _cultura) + "B";
        if (valor >= 1_000_000) return (valor / 1_000_000m).ToString("0.##", _cultura) + "M";
        if (valor >= 1_000) return (valor / 1_000m).ToString("0.##", _cultura) + "k";
        return valor.ToString("0", _cultura);
    }
}
