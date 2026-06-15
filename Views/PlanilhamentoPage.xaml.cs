using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Tela de planilhamento de balanços patrimoniais — versão multi-período.
///
/// Fluxo:
///   1. Ao abrir: carrega plano de contas e cria 1 período default (ano atual, Individual)
///   2. Usuário pode adicionar mais períodos (botão "+ Período")
///   3. Cada período vira uma coluna na tabela
///   4. Edita valores nas analíticas → totalizadoras recalculadas naquela coluna
///   5. Importar PDF: preenche o PRIMEIRO período (com aviso)
///   6. Salvar: gera 1 Balanco por período, salva todos
/// </summary>
public partial class PlanilhamentoPage : ContentPage
{
    private readonly IPlanilhamentoController _controller;
    private readonly ISessaoUsuario _sessao;

    // Estado da tela
    private readonly List<PeriodoPlanilhado> _periodos = new();
    private List<LinhaContaPlanilhamento> _linhas = new();
    private List<Empresa> _empresas = new();
    private List<ContaPadrao> _planoContas = new();
    private bool _carregadoUmaVez;

    // Empresa selecionada via busca por texto (substitui pkEmpresa.SelectedItem)
    private Empresa? _empresaSelecionada;

    // Estado da importação (preserva hash do PDF ao salvar o 1º período)
    private string? _hashPdfImportado;
    private string? _nomeArquivoImportado;
    private string _origemImportacao = "Manual";

    // PDF carregado em memória, pra permitir "tentar novamente" sem reenviar
    private byte[]? _ultimoPdfBytes;
    private string? _ultimoPdfNome;

    // DREs extraídas na importação (por ano+tipo), salvas junto com o balanço
    private readonly Dictionary<(int ano, TipoBalanco tipo), Dre> _dresImportadas = new();

    // Cores adaptativas pra construção dinâmica do Grid
    private static readonly Color _corAzulHeader = Color.FromArgb("#1F3A60");
    private static readonly Color _corVerde = Color.FromArgb("#0E7C66");
    private static readonly Color _corVermelho = Color.FromArgb("#C0392B");

    public PlanilhamentoPage(IPlanilhamentoController controller, ISessaoUsuario sessao)
    {
        InitializeComponent();
        _controller = controller;
        _sessao = sessao;

        // Popula anos no painel de adicionar período (últimos 10 anos)
        var anoAtual = DateTime.Now.Year;
        pkAddAno.ItemsSource = Enumerable.Range(anoAtual - 9, 10)
            .Reverse()
            .Select(a => a.ToString())
            .ToList();
        pkAddAno.SelectedIndex = 1;
        pkAddMes.SelectedIndex = 0;   // Ano inteiro
        pkAddTipo.SelectedIndex = 0;  // Individual
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Empresas são recarregadas SEMPRE, pra refletir cadastros feitos em
        // outras telas enquanto a página ficou viva no Shell.
        await CarregarEmpresasAsync();

        if (_carregadoUmaVez) return;
        await InicializarPlanoEPeriodoAsync();
        _carregadoUmaVez = true;
    }

    private async Task CarregarEmpresasAsync()
    {
        var rEmpresas = await _controller.ListarEmpresasAsync();
        if (rEmpresas.Sucesso && rEmpresas.Dados is not null)
            _empresas = rEmpresas.Dados.ToList();
    }

    private async Task InicializarPlanoEPeriodoAsync()
    {
        // Plano de contas
        var rContas = await _controller.ListarPlanoDeContasAsync();
        if (rContas.Sucesso && rContas.Dados is not null)
        {
            _planoContas = rContas.Dados.ToList();
            _linhas = LinhaContaPlanilhamento.ConstruirHierarquia(_planoContas);

            // Período default: ano anterior, anual, individual
            _periodos.Add(new PeriodoPlanilhado
            {
                Mes = null,
                Ano = DateTime.Now.Year - 1,
                Tipo = TipoBalanco.Individual
            });
            ReconstruirTabela();
        }
    }

    // ═════════════════════════════════════════════════════════
    // BUSCA DE EMPRESA POR TEXTO
    // ═════════════════════════════════════════════════════════
    //
    // Substituiu o Picker (que tem bug no MAUI Windows pra mostrar a seleção).
    // Funcionamento:
    //   - Usuário digita no Entry → OnBuscaEmpresaChanged filtra e mostra resultados
    //   - Foco no Entry → mostra lista (se tem termo de busca)
    //   - Clica num resultado → seleciona, escreve nome no Entry, esconde lista
    //   - _empresaSelecionada armazena a seleção (substitui pkEmpresa.SelectedItem)
    // ═════════════════════════════════════════════════════════

    private void OnBuscaEmpresaChanged(object? sender, TextChangedEventArgs e)
    {
        // Ignora mudanças disparadas quando preenchemos o campo após uma seleção
        // (evita reabrir o painel de resultados logo após escolher a empresa).
        if (_atualizandoTextoPorSelecao) return;

        var termo = (e.NewTextValue ?? string.Empty).Trim().ToLowerInvariant();

        // Se o texto não bate exatamente com a empresa selecionada, deseleciona.
        // (Permite que o usuário edite/troque sem precisar limpar manualmente.)
        if (_empresaSelecionada is not null
            && !string.Equals(termo, _empresaSelecionada.RazaoSocial, StringComparison.OrdinalIgnoreCase))
        {
            _empresaSelecionada = null;
            AtualizarSubtitulo();
        }

        if (string.IsNullOrEmpty(termo))
        {
            painelResultadosEmpresa.IsVisible = false;
            lvResultadosEmpresa.ItemsSource = null;
            return;
        }

        // Filtra por RazaoSocial OU por CNPJ (apenas dígitos do termo vs CNPJ)
        var digitosTermo = new string(termo.Where(char.IsDigit).ToArray());

        var filtradas = _empresas
            .Where(emp =>
                emp.RazaoSocial.ToLowerInvariant().Contains(termo)
                || (digitosTermo.Length >= 2 && (emp.Cnpj ?? "").Contains(digitosTermo)))
            .Take(15)
            .ToList();

        lvResultadosEmpresa.ItemsSource = filtradas;
        painelResultadosEmpresa.IsVisible = true;
    }

    private void OnBuscaEmpresaFocused(object? sender, FocusEventArgs e)
    {
        // Reabre a lista se o usuário voltou a focar com texto digitado
        if (!string.IsNullOrWhiteSpace(txtBuscaEmpresa.Text)
            && lvResultadosEmpresa.ItemsSource is not null)
        {
            painelResultadosEmpresa.IsVisible = true;
        }
    }

    private void OnEmpresaResultadoSelecionado(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Empresa empresa) return;

        _empresaSelecionada = empresa;

        // Atualiza o campo com o nome (sem disparar nova busca — usamos uma flag)
        _atualizandoTextoPorSelecao = true;
        txtBuscaEmpresa.Text = empresa.RazaoSocial;
        _atualizandoTextoPorSelecao = false;

        // Fecha o painel e limpa a seleção da CollectionView
        painelResultadosEmpresa.IsVisible = false;
        lvResultadosEmpresa.SelectedItem = null;

        lblSubtitulo.Text = $"Planilhando balanços de '{empresa.RazaoSocial}' — {_periodos.Count} período(s).";
    }

    private bool _atualizandoTextoPorSelecao = false;

    // ═════════════════════════════════════════════════════════
    // ADICIONAR / REMOVER PERÍODO
    // ═════════════════════════════════════════════════════════

    private void OnAdicionarPeriodoClicado(object? sender, EventArgs e)
    {
        painelAddPeriodo.IsVisible = !painelAddPeriodo.IsVisible;
    }

    private async void OnConfirmarAdicionarPeriodo(object? sender, EventArgs e)
    {
        var anoStr = pkAddAno.SelectedItem as string;
        if (!int.TryParse(anoStr, out var ano))
        {
            await DisplayAlert("Atenção", "Selecione um ano.", "OK");
            return;
        }

        int? mes = pkAddMes.SelectedIndex == 0 ? null : pkAddMes.SelectedIndex;
        var tipo = pkAddTipo.SelectedIndex == 1 ? TipoBalanco.Consolidado : TipoBalanco.Individual;

        var novo = new PeriodoPlanilhado { Mes = mes, Ano = ano, Tipo = tipo };

        if (_periodos.Contains(novo))
        {
            await DisplayAlert("Atenção", $"O período '{novo.LabelCompleto}' já está adicionado.", "OK");
            return;
        }

        _periodos.Add(novo);
        painelAddPeriodo.IsVisible = false;
        ReconstruirTabela();
        AtualizarSubtitulo();
    }

    private void OnCancelarAdicionarPeriodo(object? sender, EventArgs e)
    {
        painelAddPeriodo.IsVisible = false;
    }

    private void RemoverPeriodo(PeriodoPlanilhado p)
    {
        if (_periodos.Count == 1)
        {
            DisplayAlert("Atenção",
                "Pelo menos um período é obrigatório. Use o botão Limpar pra zerar valores.",
                "OK");
            return;
        }

        int idx = _periodos.IndexOf(p);
        _periodos.RemoveAt(idx);

        // Remove a coluna correspondente de todas as linhas e re-indexa as colunas posteriores
        foreach (var linha in _linhas)
        {
            linha.Celulas.Remove(idx);
            var chavesPosteriores = linha.Celulas.Keys.Where(k => k > idx).OrderBy(k => k).ToList();
            foreach (var k in chavesPosteriores)
            {
                var cel = linha.Celulas[k];
                cel.PeriodoIdx = k - 1;
                linha.Celulas[k - 1] = cel;
                linha.Celulas.Remove(k);
            }
        }

        ReconstruirTabela();
        AtualizarSubtitulo();
    }

    private void AtualizarSubtitulo()
    {
        if (_empresaSelecionada is Empresa empresa)
        {
            lblSubtitulo.Text = $"Planilhando balanços de '{empresa.RazaoSocial}' — {_periodos.Count} período(s).";
        }
        else
        {
            lblSubtitulo.Text = $"{_periodos.Count} período(s) ativo(s). Selecione uma empresa antes de salvar.";
        }
    }

    // ═════════════════════════════════════════════════════════
    // CONSTRUÇÃO DA TABELA (Grid dinâmico)
    // ═════════════════════════════════════════════════════════

    private void ReconstruirTabela()
    {
        gridTabela.Children.Clear();
        gridTabela.RowDefinitions.Clear();
        gridTabela.ColumnDefinitions.Clear();

        // Colunas: Código (110) | Descrição (380) | N x Período (170)
        gridTabela.ColumnDefinitions.Add(new ColumnDefinition(110));
        gridTabela.ColumnDefinitions.Add(new ColumnDefinition(380));
        for (int i = 0; i < _periodos.Count; i++)
            gridTabela.ColumnDefinitions.Add(new ColumnDefinition(170));

        // Monta a sequência de renderização: cada conta + um botão "+" logo após
        // a última conta analítica de cada subgrupo, pra criar novos campos naquela área.
        var elementos = new List<(LinhaContaPlanilhamento? linha, int paiIdParaAdicionar)>();
        for (int idx = 0; idx < _linhas.Count; idx++)
        {
            var l = _linhas[idx];
            elementos.Add((l, 0));
            if (l.EhEditavel && l.Pai is not null)
            {
                bool ultimaDoPai = (idx == _linhas.Count - 1)
                                   || !ReferenceEquals(_linhas[idx + 1].Pai, l.Pai);
                if (ultimaDoPai)
                    elementos.Add((null, l.Pai.Conta.Id));
            }
        }

        // Linhas: 1 header + 1 por elemento (contas + botões "+")
        gridTabela.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        foreach (var _ in elementos)
            gridTabela.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        // ──────── HEADER ────────
        AdicionarHeaderCelula(0, 0, "CÓDIGO", HorizontalOptions: LayoutOptions.Start);
        AdicionarHeaderCelula(0, 1, "DESCRIÇÃO", HorizontalOptions: LayoutOptions.Start);
        for (int i = 0; i < _periodos.Count; i++)
        {
            AdicionarHeaderCelula(0, 2 + i, _periodos[i].LabelCompleto,
                                  HorizontalOptions: LayoutOptions.End);
        }

        // ──────── BODY ────────
        int gridRow = 0;
        foreach (var (linhaElem, paiIdAdd) in elementos)
        {
            gridRow++;

            // Marcador de botão "+" (adicionar conta no subgrupo)
            if (linhaElem is null)
            {
                AdicionarBotaoNovaConta(gridRow, paiIdAdd);
                continue;
            }

            var linha = linhaElem;
            bool ehTotal = linha.EhTotalizadora;

            // Fundo da linha (totalizadoras destacadas)
            var bgRow = new Grid
            {
                BackgroundColor = ehTotal
                    ? Application.Current!.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#243140")
                        : Color.FromArgb("#EEF2F7")
                    : Colors.Transparent
            };
            Grid.SetRow(bgRow, gridRow);
            Grid.SetColumnSpan(bgRow, 2 + _periodos.Count);
            gridTabela.Children.Add(bgRow);

            // Código
            var labelCodigo = new Label
            {
                Text = linha.Codigo,
                FontFamily = "Consolas",
                FontSize = 12,
                FontAttributes = linha.Estilo,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.NoWrap,
                Padding = new Thickness(12, 8, 8, 8)
            };
            labelCodigo.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#5A6478"), Color.FromArgb("#A8B0BF"));
            Grid.SetRow(labelCodigo, gridRow);
            Grid.SetColumn(labelCodigo, 0);
            gridTabela.Children.Add(labelCodigo);

            // Descrição (com indentação)
            var labelDesc = new Label
            {
                Text = linha.Descricao,
                FontSize = 14,
                FontAttributes = linha.Estilo,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
                Padding = new Thickness(linha.Indentacao.Left, 8, 8, 8)
            };
            labelDesc.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));
            Grid.SetRow(labelDesc, gridRow);
            Grid.SetColumn(labelDesc, 1);
            gridTabela.Children.Add(labelDesc);

            // Células de valor (1 por período)
            for (int p = 0; p < _periodos.Count; p++)
            {
                var celula = linha.ObterCelula(p);

                if (ehTotal)
                {
                    // Totalizadora: label read-only
                    var lbl = new Label
                    {
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.End,
                        VerticalOptions = LayoutOptions.Center,
                        Padding = new Thickness(8, 8, 16, 8)
                    };
                    lbl.SetAppThemeColor(Label.TextColorProperty,
                        Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));
                    lbl.SetBinding(Label.TextProperty, new Binding(nameof(CelulaPlanilhamento.ValorFormatado), source: celula));
                    Grid.SetRow(lbl, gridRow);
                    Grid.SetColumn(lbl, 2 + p);
                    gridTabela.Children.Add(lbl);
                }
                else
                {
                    // Analítica: Border com Entry editável
                    var border = new Border
                    {
                        StrokeThickness = 1,
                        Padding = 0,
                        HeightRequest = 32,
                        Margin = new Thickness(6, 6, 16, 6),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 }
                    };
                    border.SetAppThemeColor(Border.StrokeProperty,
                        Color.FromArgb("#C5CCD6"), Color.FromArgb("#3A4554"));
                    border.SetAppThemeColor(Border.BackgroundColorProperty,
                        Colors.White, Color.FromArgb("#13202B"));

                    var entry = new Entry
                    {
                        Placeholder = "0,00",
                        Keyboard = Keyboard.Numeric,
                        HorizontalTextAlignment = TextAlignment.End,
                        FontSize = 13,
                        BackgroundColor = Colors.Transparent,
                        Margin = new Thickness(8, 0),
                        // Texto inicial formatado pra display
                        Text = celula.Valor == 0 ? string.Empty : celula.ValorFormatado
                    };

                    // ─────────────────────────────────────────────────────────────
                    // CONTROLE MANUAL DO TEXTO (sem TwoWay binding):
                    //
                    // - TextChanged: parseia o texto digitado e atualiza Valor SEM
                    //   alterar o Entry de volta (evita "2" → "2,00" interferindo
                    //   na digitação de "2000000")
                    // - Unfocused: quando usuário sai do campo, formata pra
                    //   "2.000.000,00"
                    // - PropertyChanged em celula: se Valor é alterado externamente
                    //   (importação PDF, recálculo de totalizadora), atualiza o
                    //   Entry — MAS só se ele não estiver focado, pra não
                    //   interromper digitação
                    // ─────────────────────────────────────────────────────────────
                    var celulaRef = celula;
                    var entryRef = entry;
                    bool _atualizandoDoUsuario = false;

                    entry.TextChanged += (s, args) =>
                    {
                        _atualizandoDoUsuario = true;
                        var v = CelulaPlanilhamento.TentarParsear(args.NewTextValue);
                        if (v.HasValue)
                        {
                            // Atualiza Valor sem disparar refresh do Entry
                            // (a propagação pra totalizadora acontece via setter de Valor)
                            celulaRef.Valor = v.Value;
                        }
                        _atualizandoDoUsuario = false;
                    };

                    entry.Unfocused += (s, args) =>
                    {
                        // Formata pra apresentação após edição
                        entryRef.Text = celulaRef.Valor == 0 ? string.Empty : celulaRef.ValorFormatado;
                    };

                    celula.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(CelulaPlanilhamento.Valor))
                        {
                            // Atualiza KPIs sempre
                            AtualizarKPIs();

                            // Atualiza Entry apenas se NÃO está focado e a mudança
                            // veio de fora (importação, recálculo) — não de digitação
                            if (!entryRef.IsFocused && !_atualizandoDoUsuario)
                            {
                                entryRef.Text = celulaRef.Valor == 0
                                    ? string.Empty
                                    : celulaRef.ValorFormatado;
                            }
                        }
                    };

                    border.Content = entry;
                    Grid.SetRow(border, gridRow);
                    Grid.SetColumn(border, 2 + p);
                    gridTabela.Children.Add(border);
                }
            }

            // Borda inferior fina
            var bordaBaixo = new BoxView
            {
                HeightRequest = 1,
                VerticalOptions = LayoutOptions.End
            };
            bordaBaixo.SetAppThemeColor(BoxView.ColorProperty,
                Color.FromArgb("#E1E5EB"), Color.FromArgb("#2A3441"));
            Grid.SetRow(bordaBaixo, gridRow);
            Grid.SetColumnSpan(bordaBaixo, 2 + _periodos.Count);
            gridTabela.Children.Add(bordaBaixo);
        }

        AtualizarChips();
        AtualizarKPIs();
    }

    /// <summary>
    /// Renderiza uma linha com um botão "+" discreto na coluna de descrição,
    /// que permite criar uma nova conta analítica no subgrupo (conta-pai) dado.
    /// </summary>
    private void AdicionarBotaoNovaConta(int gridRow, int contaPaiId)
    {
        var btn = new Button
        {
            Text = "+ Adicionar conta",
            FontSize = 12,
            HeightRequest = 30,
            Padding = new Thickness(10, 0),
            HorizontalOptions = LayoutOptions.Start,
            Margin = new Thickness(24, 2, 0, 4),
            BackgroundColor = Colors.Transparent
        };
        btn.SetAppThemeColor(Button.TextColorProperty,
            Color.FromArgb("#1F6FEB"), Color.FromArgb("#5A9BFF"));
        btn.Clicked += async (s, e) => await AdicionarContaAsync(contaPaiId);

        Grid.SetRow(btn, gridRow);
        Grid.SetColumn(btn, 1);
        gridTabela.Children.Add(btn);
    }

    /// <summary>
    /// Pede a descrição, cria a conta analítica (código único gerado pela área),
    /// recarrega o plano preservando os valores já digitados e reconstrói a tabela.
    /// </summary>
    private async Task AdicionarContaAsync(int contaPaiId)
    {
        var descricao = await DisplayPromptAsync(
            "Nova conta",
            "Descrição da nova conta (o código é gerado automaticamente conforme a área):",
            accept: "Criar", cancel: "Cancelar",
            placeholder: "ex: Aplicações de Curto Prazo");

        if (string.IsNullOrWhiteSpace(descricao)) return;

        var r = await _controller.CriarContaAnaliticaAsync(contaPaiId, descricao.Trim());
        if (!r.Sucesso || r.Dados is null)
        {
            await DisplayAlert("Erro", r.Mensagem, "OK");
            return;
        }

        await RecarregarPlanoPreservandoValoresAsync();
        await DisplayAlert("Conta criada",
            $"'{r.Dados.Codigo} — {r.Dados.Descricao}' foi adicionada e já aparece na tabela.", "OK");
    }

    /// <summary>
    /// Recarrega o plano de contas do banco (pra incluir contas recém-criadas)
    /// sem perder os valores que o usuário já digitou em cada período.
    /// </summary>
    private async Task RecarregarPlanoPreservandoValoresAsync()
    {
        // Snapshot dos valores atuais: (contaId, períodoIdx) -> valor
        var snapshot = new Dictionary<(int contaId, int perIdx), decimal>();
        foreach (var linha in _linhas.Where(l => l.EhEditavel))
            foreach (var kv in linha.Celulas)
                if (kv.Value.Valor != 0)
                    snapshot[(linha.Conta.Id, kv.Key)] = kv.Value.Valor;

        var rContas = await _controller.ListarPlanoDeContasAsync();
        if (!rContas.Sucesso || rContas.Dados is null) return;

        _planoContas = rContas.Dados.ToList();
        _linhas = LinhaContaPlanilhamento.ConstruirHierarquia(_planoContas);

        // Restaura valores
        foreach (var ((contaId, perIdx), valor) in snapshot)
        {
            if (perIdx >= _periodos.Count) continue;
            var linha = _linhas.FirstOrDefault(l => l.Conta.Id == contaId);
            if (linha is not null && linha.EhEditavel)
                linha.ObterCelula(perIdx).Valor = valor;
        }

        ReconstruirTabela();
    }

    /// <summary>Adiciona uma célula de header (fundo azul corporativo) no Grid.</summary>
    private void AdicionarHeaderCelula(int row, int col, string texto, LayoutOptions HorizontalOptions)
    {
        var bg = new BoxView { Color = _corAzulHeader };
        Grid.SetRow(bg, row);
        Grid.SetColumn(bg, col);
        gridTabela.Children.Add(bg);

        var lbl = new Label
        {
            Text = texto,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = HorizontalOptions,
            Padding = HorizontalOptions == LayoutOptions.End
                ? new Thickness(8, 12, 16, 12)
                : new Thickness(12, 12, 8, 12)
        };
        Grid.SetRow(lbl, row);
        Grid.SetColumn(lbl, col);
        gridTabela.Children.Add(lbl);
    }

    // ═════════════════════════════════════════════════════════
    // CHIPS DOS PERÍODOS ATIVOS
    // ═════════════════════════════════════════════════════════

    private void AtualizarChips()
    {
        layoutChips.Children.Clear();
        painelChips.IsVisible = _periodos.Count > 0;

        for (int i = 0; i < _periodos.Count; i++)
        {
            var p = _periodos[i];
            var chip = new Border
            {
                StrokeThickness = 1,
                Padding = new Thickness(12, 4),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }
            };
            chip.SetAppThemeColor(Border.StrokeProperty,
                Color.FromArgb("#C5CCD6"), Color.FromArgb("#3A4554"));
            chip.SetAppThemeColor(Border.BackgroundColorProperty,
                Color.FromArgb("#F4F6F9"), Color.FromArgb("#243140"));

            var hbox = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };

            var lbl = new Label
            {
                Text = p.LabelCompleto,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };
            lbl.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));

            var btnX = new Button
            {
                Text = "x",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 22,
                HeightRequest = 22,
                Padding = 0,
                BackgroundColor = Colors.Transparent,
                CornerRadius = 11
            };
            btnX.SetAppThemeColor(Button.TextColorProperty,
                Color.FromArgb("#5A6478"), Color.FromArgb("#A8B0BF"));
            var pRef = p;
            btnX.Clicked += (s, e) => RemoverPeriodo(pRef);

            hbox.Children.Add(lbl);
            hbox.Children.Add(btnX);
            chip.Content = hbox;
            layoutChips.Children.Add(chip);
        }
    }

    // ═════════════════════════════════════════════════════════
    // KPIs (totais por período)
    // ═════════════════════════════════════════════════════════

    private void AtualizarKPIs()
    {
        layoutKPIs.Children.Clear();
        var cultura = new CultureInfo("pt-BR");

        // Cada período gera UMA LINHA compacta com tudo numa fileira:
        //   "2025 (I)   Ativo: R$ 1.234,56   P+PL: R$ 1.234,56   Δ R$ 0,00"
        // Isso economiza ~70% da altura do rodapé vs cards verticais.
        for (int p = 0; p < _periodos.Count; p++)
        {
            var ativo = SomarGrupoNaColuna(GrupoContaPrincipal.Ativo, p);
            var passivo = SomarGrupoNaColuna(GrupoContaPrincipal.Passivo, p);
            var pl = SomarGrupoNaColuna(GrupoContaPrincipal.PatrimonioLiquido, p);
            var dif = ativo - (passivo + pl);
            var fecha = Math.Abs(dif) <= 1.00m;

            var linha = new HorizontalStackLayout { Spacing = 20, VerticalOptions = LayoutOptions.Center };

            // Label do período (bold, com cor secundária)
            var lblPer = new Label
            {
                Text = _periodos[p].LabelCompleto,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                CharacterSpacing = 1,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 80
            };
            lblPer.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#5A6478"), Color.FromArgb("#A8B0BF"));

            var lblAtivo = new Label
            {
                Text = $"Ativo: R$ {ativo.ToString("N2", cultura)}",
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            lblAtivo.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));

            var lblPpl = new Label
            {
                Text = $"P+PL: R$ {(passivo + pl).ToString("N2", cultura)}",
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center
            };
            lblPpl.SetAppThemeColor(Label.TextColorProperty,
                Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));

            var lblDif = new Label
            {
                Text = $"Δ R$ {dif.ToString("N2", cultura)}",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = fecha ? _corVerde : _corVermelho,
                VerticalOptions = LayoutOptions.Center
            };

            linha.Children.Add(lblPer);
            linha.Children.Add(lblAtivo);
            linha.Children.Add(lblPpl);
            linha.Children.Add(lblDif);
            layoutKPIs.Children.Add(linha);
        }
    }

    private decimal SomarGrupoNaColuna(GrupoContaPrincipal grupo, int perIdx)
    {
        return _linhas
            .Where(l => l.Grupo == grupo && !l.EhTotalizadora)
            .Sum(l => l.ObterCelula(perIdx).Valor);
    }

    // ═════════════════════════════════════════════════════════
    // IMPORTAÇÃO DE PDF (preenche só o primeiro período)
    // ═════════════════════════════════════════════════════════

    private async void OnImportarPdfClicado(object? sender, EventArgs e)
        => await ImportarPdfHeuristicoAsync();

    private async void OnImportarPdfComIaClicado(object? sender, EventArgs e)
        => await ImportarComIaAutomaticoAsync();

    // ─────────────────────────────────────────────────────────
    // IMPORTAÇÃO HEURÍSTICA (parser regex) — preenche 1º período
    // ─────────────────────────────────────────────────────────

    private async Task ImportarPdfHeuristicoAsync()
    {
        if (_empresaSelecionada is null)
        {
            await DisplayAlert("Atenção", "Selecione uma empresa antes de importar.", "OK");
            return;
        }
        if (_periodos.Count == 0)
        {
            await DisplayAlert("Atenção", "Adicione pelo menos um período antes de importar.", "OK");
            return;
        }
        if (_periodos.Count > 1)
        {
            var ok = await DisplayAlert("Importar PDF",
                $"A importação simples preenche apenas o PRIMEIRO período ({_periodos[0].LabelCompleto}). " +
                "Pra detectar empresa e múltiplos períodos automaticamente, use 'Importar com IA'. Continuar?",
                "Continuar", "Cancelar");
            if (!ok) return;
        }

        var arquivo = await SelecionarPdfAsync();
        if (arquivo is null) return;

        try
        {
            await using var stream = await arquivo.OpenReadAsync();
            var resultado = await _controller.ImportarPdfAsync(stream, arquivo.FileName);

            if (!resultado.Sucesso || resultado.Dados is null)
            {
                AtualizarSubtitulo();
                await DisplayAlert("Erro na análise", resultado.Mensagem, "OK");
                return;
            }

            int aplicadas = 0;
            foreach (var (contaId, valor) in resultado.Dados.ContasMapeadas)
            {
                var linha = _linhas.FirstOrDefault(l => l.Conta.Id == contaId);
                if (linha is not null && linha.EhEditavel)
                {
                    linha.ObterCelula(0).Valor = valor;
                    aplicadas++;
                }
            }

            _hashPdfImportado = resultado.Dados.HashPdf;
            _nomeArquivoImportado = arquivo.FileName;
            _origemImportacao = "PDF";

            ReconstruirTabela();
            await DisplayAlert("PDF importado",
                $"{aplicadas} contas preenchidas no período {_periodos[0].LabelCompleto}.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao processar PDF: {ex.Message}", "OK");
            AtualizarSubtitulo();
        }
    }

    // ─────────────────────────────────────────────────────────
    // IMPORTAÇÃO AUTOMÁTICA VIA IA — faz TUDO sozinha:
    // detecta/cadastra empresa, cria períodos, define tipos, preenche contas
    // ─────────────────────────────────────────────────────────

    private void DefinirEmpresaSelecionada(Empresa empresa)
    {
        _empresaSelecionada = empresa;
        _atualizandoTextoPorSelecao = true;
        txtBuscaEmpresa.Text = empresa.RazaoSocial;
        _atualizandoTextoPorSelecao = false;
    }

    /// <summary>
    /// Cadastro rápido de empresa por pop-up (apenas nome e CNPJ). Usado quando a
    /// IA consegue ler o balanço mas não identifica a empresa (ex.: CNPJ ausente
    /// no PDF). Retorna a empresa cadastrada/encontrada, ou null se o usuário
    /// cancelar. Em caso de erro de validação, deixa corrigir e tentar de novo.
    /// </summary>
    private async Task<Empresa?> CadastrarEmpresaRapidaAsync(
        string? nomeSugerido, string? cnpjSugerido, TipoEmpresa tipo, string? uf)
    {
        var quer = await DisplayAlert("Empresa não identificada",
            "A IA leu o balanço, mas não conseguiu identificar a empresa automaticamente. " +
            "Deseja cadastrá-la agora informando nome e CNPJ?",
            "Cadastrar", "Agora não");
        if (!quer) return null;

        while (true)
        {
            var nome = await DisplayPromptAsync("Cadastro rápido de empresa",
                "Nome / Razão social:", accept: "Próximo", cancel: "Cancelar",
                initialValue: nomeSugerido ?? string.Empty, maxLength: 200);
            if (string.IsNullOrWhiteSpace(nome)) return null; // cancelou

            var cnpj = await DisplayPromptAsync("Cadastro rápido de empresa",
                "CNPJ (14 dígitos):", accept: "Cadastrar", cancel: "Cancelar",
                initialValue: cnpjSugerido ?? string.Empty,
                keyboard: Keyboard.Numeric, maxLength: 18);
            if (cnpj is null) return null; // cancelou

            var r = await _controller.BuscarOuCadastrarEmpresaAsync(nome.Trim(), cnpj.Trim(), tipo, uf);
            if (r.Sucesso && r.Dados is not null)
            {
                DefinirEmpresaSelecionada(r.Dados);
                if (_empresas.All(e => e.Id != r.Dados.Id))
                    _empresas.Add(r.Dados); // reflete na busca local na hora
                return r.Dados;
            }

            var tentar = await DisplayAlert("Não foi possível cadastrar",
                r.Mensagem + "\n\nDeseja corrigir os dados e tentar de novo?",
                "Tentar de novo", "Cancelar");
            if (!tentar) return null;
            nomeSugerido = nome;   // mantém o que já foi digitado
            cnpjSugerido = cnpj;
        }
    }

    private async Task ImportarComIaAutomaticoAsync()
    {
        if (!await _controller.IaConfiguradaAsync())
        {
            await DisplayAlert("Gemini não configurado",
                "Configure a API key do Google na 'Área de Testes' antes de usar a importação por IA.",
                "OK");
            return;
        }

        var arquivo = await SelecionarPdfAsync();
        if (arquivo is null) return;

        // Lê o PDF em memória UMA vez, pra poder tentar de novo sem reenviar
        try
        {
            await using var stream = await arquivo.OpenReadAsync();
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem);
            _ultimoPdfBytes = mem.ToArray();
            _ultimoPdfNome = arquivo.FileName;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Não foi possível ler o PDF: {ex.Message}", "OK");
            return;
        }

        await ExecutarImportacaoIaAsync();
    }

    /// <summary>
    /// Executa a análise do PDF já carregado em memória (_ultimoPdfBytes).
    /// Em caso de falha (ex: erro 503/sobrecarga), oferece "Tentar novamente"
    /// reusando os mesmos bytes — sem o usuário precisar reenviar o arquivo.
    /// </summary>
    private async Task ExecutarImportacaoIaAsync()
    {
        if (_ultimoPdfBytes is null || _ultimoPdfNome is null) return;

        lblSubtitulo.Text = $"Analisando '{_ultimoPdfNome}' via Gemini — detectando empresa e períodos...";

        try
        {
            using var stream = new MemoryStream(_ultimoPdfBytes);
            var r = await _controller.ImportarPdfAutomaticoAsync(stream, _ultimoPdfNome);

            if (!r.Sucesso || r.Dados is null)
            {
                AtualizarSubtitulo();
                var tentar = await DisplayAlert("Erro na análise",
                    $"{r.Mensagem}\n\nDeseja tentar novamente? (o PDF já está carregado, não precisa reenviar)",
                    "Tentar novamente", "Cancelar");
                if (tentar) await ExecutarImportacaoIaAsync();
                return;
            }

            var dados = r.Dados;

            if (dados.Periodos.Count == 0)
            {
                AtualizarSubtitulo();
                await DisplayAlert("Nada detectado",
                    "A IA não conseguiu detectar períodos no PDF. " +
                    string.Join(" ", dados.Avisos), "OK");
                return;
            }

            // 1) Empresa: busca ou cadastra automaticamente
            var empresaR = await _controller.BuscarOuCadastrarEmpresaAsync(
                dados.RazaoSocial, dados.Cnpj, dados.TipoEmpresa, dados.UfAtuacao);

            string statusEmpresa;
            if (empresaR.Sucesso && empresaR.Dados is not null)
            {
                DefinirEmpresaSelecionada(empresaR.Dados);
                statusEmpresa = empresaR.Mensagem ?? "Empresa definida.";
            }
            else
            {
                // A IA leu o balanço mas não conseguiu cadastrar a empresa sozinha
                // (quase sempre porque o CNPJ não estava legível no PDF).
                // Oferece o cadastro rápido por pop-up (nome + CNPJ).
                var cadastrada = await CadastrarEmpresaRapidaAsync(
                    dados.RazaoSocial, dados.Cnpj, dados.TipoEmpresa, dados.UfAtuacao);
                statusEmpresa = cadastrada is not null
                    ? "Empresa cadastrada manualmente."
                    : "Empresa não definida — selecione ou cadastre antes de salvar.";
            }

            // 2) Recarrega o plano de contas (a IA pode ter criado contas novas/ajuste)
            await RecarregarPlanoPreservandoValoresAsync();

            // 3) Limpa períodos e células atuais
            _periodos.Clear();
            foreach (var linha in _linhas)
                linha.Celulas.Clear();

            // 4) Cria períodos detectados + preenche contas
            _dresImportadas.Clear();
            for (int i = 0; i < dados.Periodos.Count; i++)
            {
                var pd = dados.Periodos[i];
                _periodos.Add(new PeriodoPlanilhado { Ano = pd.Ano, Mes = pd.Mes, Tipo = pd.Tipo });

                foreach (var (contaId, valor) in pd.ContasMapeadas)
                {
                    var linha = _linhas.FirstOrDefault(l => l.Conta.Id == contaId);
                    if (linha is not null && linha.EhEditavel)
                        linha.ObterCelula(i).Valor = valor;
                }

                // Guarda a DRE (se a IA extraiu) pra salvar junto com o balanço
                if (pd.TemDre)
                {
                    _dresImportadas[(pd.Ano, pd.Tipo)] = new Dre
                    {
                        AnoExercicio = pd.Ano,
                        TipoBalanco = pd.Tipo,
                        ReceitaLiquida = pd.DreReceitaLiquida,
                        LucroBruto = pd.DreLucroBruto,
                        ResultadoOperacional = pd.DreResultadoOperacional,
                        DespesasFinanceiras = pd.DreDespesasFinanceiras,
                        LucroLiquido = pd.DreLucroLiquido,
                        Origem = "PDF + IA (automático)",
                        HashOrigemPdf = (i == 0) ? dados.HashPdf : null
                    };
                }
            }

            _hashPdfImportado = dados.HashPdf;
            _nomeArquivoImportado = _ultimoPdfNome;
            _origemImportacao = "PDF + IA (automático)";

            // 5) Reconstrói a tabela com tudo preenchido
            ReconstruirTabela();

            // 6) Abre a tela de revisão lado a lado (extraído x impresso por grupo).
            //    A tabela do planilhamento já está preenchida por baixo.
            var statusInfo = statusEmpresa;
            if (!string.IsNullOrWhiteSpace(statusInfo))
                dados.Avisos.Insert(0, statusInfo);

            var revisao = App.Services.GetRequiredService<RevisaoImportacaoPage>();
            revisao.Inicializar(dados);
            await Navigation.PushAsync(revisao);
        }
        catch (Exception ex)
        {
            AtualizarSubtitulo();
            var tentar = await DisplayAlert("Erro",
                $"Falha ao processar PDF: {ex.Message}\n\nDeseja tentar novamente?",
                "Tentar novamente", "Cancelar");
            if (tentar) await ExecutarImportacaoIaAsync();
        }
    }

    /// <summary>Abre o seletor de arquivo PDF. Retorna null se cancelado/erro.</summary>
    private async Task<FileResult?> SelecionarPdfAsync()
    {
        try
        {
            return await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecionar PDF de balanço",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = new[] { ".pdf" }
                })
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Não foi possível abrir o seletor: {ex.Message}", "OK");
            return null;
        }
    }

    // ═════════════════════════════════════════════════════════
    // LIMPAR / SALVAR
    // ═════════════════════════════════════════════════════════

    private async void OnLimparClicado(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Limpar valores",
            "Tem certeza? Todos os valores digitados serão apagados (os períodos permanecem).",
            "Limpar", "Cancelar");
        if (!ok) return;

        foreach (var linha in _linhas)
        {
            foreach (var (_, cel) in linha.Celulas)
                cel.Valor = 0;
        }
        _hashPdfImportado = null;
        _nomeArquivoImportado = null;
        _origemImportacao = "Manual";
        AtualizarKPIs();
    }

    private async void OnSalvarClicado(object? sender, EventArgs e)
    {
        if (_empresaSelecionada is not Empresa empresa)
        {
            await DisplayAlert("Atenção", "Selecione uma empresa.", "OK");
            return;
        }
        if (_sessao.UsuarioAtual is null)
        {
            await DisplayAlert("Erro", "Sessão expirou. Faça login novamente.", "OK");
            return;
        }
        if (_periodos.Count == 0)
        {
            await DisplayAlert("Atenção", "Adicione pelo menos um período.", "OK");
            return;
        }

        // Verifica antecipadamente quais períodos já têm balanço salvo, pra
        // oferecer substituição em vez de simplesmente falhar.
        var jaExistem = new List<string>();
        foreach (var per in _periodos)
        {
            if (await _controller.ExisteBalancoAsync(empresa.Id, per.Ano, per.Tipo))
                jaExistem.Add(per.LabelCompleto);
        }

        bool substituir = false;
        if (jaExistem.Count > 0)
        {
            substituir = await DisplayAlert(
                "Balanço já existe",
                $"Já existe(m) balanço(s) salvo(s) para:\n• {string.Join("\n• ", jaExistem)}\n\n" +
                "Deseja SUBSTITUIR? O balanço anterior não será apagado — fica guardado " +
                "no banco (inativo) e pode ser recuperado depois, se preciso.",
                "Substituir", "Cancelar");

            if (!substituir) return; // usuário preferiu não mexer
        }

        // Salva 1 Balanco por período
        int salvos = 0;
        var erros = new List<string>();

        for (int p = 0; p < _periodos.Count; p++)
        {
            var per = _periodos[p];

            // Só persiste contas COM valor (≠ 0). Salvar contas zeradas poluía o
            // banco e podia dar a impressão de "balanço vazio" na visualização.
            var contas = _linhas
                .Where(l => l.EhEditavel && l.ObterCelula(p).Valor != 0)
                .Select(l => new ContaBalanco
                {
                    ContaPadraoId = l.Conta.Id,
                    Valor = l.ObterCelula(p).Valor
                })
                .ToList();

            if (contas.Count == 0)
            {
                erros.Add($"{per.LabelCompleto}: nenhuma conta preenchida (todos os valores estão zerados).");
                continue;
            }

            var balanco = new Balanco
            {
                EmpresaId = empresa.Id,
                AnoExercicio = per.Ano,
                DataReferencia = per.DataReferencia,
                TipoBalanco = per.Tipo,
                UsuarioId = _sessao.UsuarioAtual.Id,
                Origem = _origemImportacao,
                HashOrigemPdf = (p == 0) ? _hashPdfImportado : null,
                Contas = contas
            };

            var r = await _controller.SalvarAsync(balanco, substituir);
            if (r.Sucesso)
            {
                salvos++;

                // Se há DRE importada pra este período, salva também
                if (_dresImportadas.TryGetValue((per.Ano, per.Tipo), out var dre) && dre.TemDados)
                {
                    dre.EmpresaId = empresa.Id;
                    dre.UsuarioId = _sessao.UsuarioAtual.Id;
                    await _controller.SalvarDreAsync(dre);
                }
            }
            else
                erros.Add($"{per.LabelCompleto}: {r.Mensagem}");
        }

        if (erros.Count == 0)
        {
            await DisplayAlert("Sucesso",
                $"{salvos} balanço(s) salvo(s) com sucesso pra '{empresa.RazaoSocial}'.", "OK");
        }
        else
        {
            var msg = $"{salvos} salvos, {erros.Count} com erro:\n\n" + string.Join("\n", erros);
            await DisplayAlert("Concluído com erros", msg, "OK");
        }
    }
}
