import re

with open('ViewModels/AnalisesViewModel.cs', 'r', encoding='utf-8') as f:
    code = f.read()

method = '''
    partial void OnEmpresaSelecionadaChanged(Empresa? value)
    {
        if (value is not null)
        {
            _ = CarregarAnaliseAsync(value);
        }
        else
        {
            IsPainelVazioVisivel = true;
            IsPainelAnaliseVisivel = false;
            Subtitulo = "Selecione uma empresa para visualizar a análise gráfica e os KPIs.";
            MensagemVazia = "Nenhuma empresa selecionada.";
        }
    }

    private async Task CarregarAnaliseAsync(Empresa empresa)
    {
        try
        {
            IsPainelVazioVisivel = true;
            IsPainelAnaliseVisivel = false;
            MensagemVazia = $"Carregando análises de {empresa.RazaoSocial}...";
            Subtitulo = $"Analisando {empresa.RazaoSocial}...";
            
            var r = await _controller.AnalisarAsync(empresa.Id);
            
            if (!r.Sucesso || r.Dados is null)
            {
                MensagemVazia = r.Mensagem;
                return;
            }

            var analise = r.Dados;

            if (analise.Evolucao.Count == 0)
            {
                MensagemVazia = $"A empresa {empresa.RazaoSocial} não possui balanços fechados.";
                return;
            }

            Subtitulo = $"Análise do Balanço {analise.TipoAnalisado} - Último exercício: {analise.Evolucao.Last().Ano}";

            // Preencher KPIs
            var ultimoInd = analise.Indicadores.LastOrDefault();
            if (ultimoInd != null)
            {
                KpiAno = ultimoInd.Ano.ToString();
                KpiLiquidez = ultimoInd.LiquidezCorrente?.ToString("N2") ?? "-";
                KpiEndividamento = ultimoInd.EndividamentoGeral?.ToString("P1") ?? "-";
                KpiComposicao = ultimoInd.ComposicaoEndividamento?.ToString("P1") ?? "-";
                KpiImobilizacao = ultimoInd.ImobilizacaoPL?.ToString("P1") ?? "-";
            }
            var ultimoEv = analise.Evolucao.LastOrDefault();
            if (ultimoEv != null)
            {
                KpiAtivo = FormatarMoedaCurto(ultimoEv.AtivoTotal);
                KpiPassivo = FormatarMoedaCurto(ultimoEv.PassivoTotal);
                KpiPL = FormatarMoedaCurto(ultimoEv.PatrimonioLiquido);
            }

            // Preencher Gráficos
            MontarGraficos(analise);

            IsPainelVazioVisivel = false;
            IsPainelAnaliseVisivel = true;
            
            // Avisar a view para renderizar os cards dinâmicos
            RenderizarRiscoDetalhesAction?.Invoke(analise);
        }
        catch (Exception ex)
        {
            MensagemVazia = $"Erro ao carregar análise: {ex.Message}";
        }
    }

    private void MontarGraficos(AnaliseEmpresa analise)
    {
        // Evolução
        var anos = analise.Evolucao.Select(e => e.Ano.ToString()).ToArray();
        
        SeriesEvolucao = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Values = analise.Evolucao.Select(e => e.AtivoTotal).ToArray(),
                Name = "Ativo Total",
                GeometrySize = 8,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                Fill = null
            },
            new LineSeries<decimal>
            {
                Values = analise.Evolucao.Select(e => e.PassivoTotal).ToArray(),
                Name = "Passivo Total",
                GeometrySize = 8,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
                Fill = null
            },
            new LineSeries<decimal>
            {
                Values = analise.Evolucao.Select(e => e.PatrimonioLiquido).ToArray(),
                Name = "Patrimônio Líquido",
                GeometrySize = 8,
                LineSmoothness = 0,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                Fill = null
            }
        };

        XAxesEvolucao = new Axis[] { new Axis { Labels = anos, LabelsRotation = 0 } };
        YAxesEvolucao = new Axis[] { new Axis { Labeler = v => FormatarMoedaCurto((decimal)v) } };

        // Composição Ativo (Pizza)
        if (analise.ComposicaoAtivo != null)
        {
            TituloComposicaoAtivo = analise.ComposicaoAtivo.Titulo;
            var seriesAtivo = new List<ISeries>();
            foreach (var f in analise.ComposicaoAtivo.Fatias)
            {
                seriesAtivo.Add(new PieSeries<decimal>
                {
                    Values = new[] { f.Valor },
                    Name = f.Rotulo,
                    DataLabelsFormatter = point => $"{f.Rotulo}: {FormatarMoedaCurto(point.Coordinate.PrimaryValue)}"
                });
            }
            SeriesCompAtivo = seriesAtivo.ToArray();
        }

        // Composição Passivo (Pizza)
        if (analise.ComposicaoPassivoPL != null)
        {
            TituloComposicaoPassivo = analise.ComposicaoPassivoPL.Titulo;
            var seriesPassivo = new List<ISeries>();
            foreach (var f in analise.ComposicaoPassivoPL.Fatias)
            {
                seriesPassivo.Add(new PieSeries<decimal>
                {
                    Values = new[] { f.Valor },
                    Name = f.Rotulo,
                    DataLabelsFormatter = point => $"{f.Rotulo}: {FormatarMoedaCurto(point.Coordinate.PrimaryValue)}"
                });
            }
            SeriesCompPassivo = seriesPassivo.ToArray();
        }

        // Liquidez (Barras)
        SeriesLiquidez = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Values = analise.Indicadores.Select(i => i.LiquidezCorrente ?? 0).ToArray(),
                Name = "Liquidez Corrente"
            },
            new ColumnSeries<decimal>
            {
                Values = analise.Indicadores.Select(i => i.LiquidezSeca ?? 0).ToArray(),
                Name = "Liquidez Seca"
            }
        };
        XAxesLiquidez = new Axis[] { new Axis { Labels = anos, LabelsRotation = 0 } };
    }

    private string FormatarMoedaCurto(decimal valor)
    {
        var v = (double)valor;
        if (Math.Abs(v) >= 1_000_000_000) return $"R$ {v / 1_000_000_000:0.##}B";
        if (Math.Abs(v) >= 1_000_000) return $"R$ {v / 1_000_000:0.##}M";
        if (Math.Abs(v) >= 1_000) return $"R$ {v / 1_000:0.##}k";
        return $"R$ {v:0.##}";
    }
'''

code = code.replace('    public async Task InicializarAsync()', method + '\n    public async Task InicializarAsync()')

with open('ViewModels/AnalisesViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(code)
