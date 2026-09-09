using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Visualização detalhada de um balanço salvo, com edição dos valores das
/// contas. Aberta como modal a partir da BalancosEmpresaPage (toque no card).
/// </summary>
public partial class DetalheBalancoPage : ContentPage
{
    private readonly IBalancoController _balancoController;
    private readonly IExportacaoController _exportacao;
    private readonly ISessaoUsuario _sessao;
    private readonly IDreDao _dreDao;
    private static readonly CultureInfo _ptBR = new("pt-BR");

    private int _balancoId;
    private Balanco? _balanco;
    private Dre? _dre;

    // Mapeia conta_padrao_id -> Entry, pra ler os valores editados ao salvar
    private readonly Dictionary<int, Entry> _entries = new();

    private static readonly Color _corVerde = Color.FromArgb("#0E7C66");
    private static readonly Color _corVermelho = Color.FromArgb("#C0392B");

    public DetalheBalancoPage(IBalancoController balancoController, IExportacaoController exportacao,
        ISessaoUsuario sessao, IDreDao dreDao)
    {
        InitializeComponent();
        _balancoController = balancoController;
        _exportacao = exportacao;
        _sessao = sessao;
        _dreDao = dreDao;
        AtualizarEstiloAbas(balancoAtivo: true);
    }

    public void Inicializar(int balancoId) => _balancoId = balancoId;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var r = await _balancoController.CarregarCompletoAsync(_balancoId);
        if (!r.Sucesso || r.Dados is null)
        {
            lblSubtitulo.Text = "Erro ao carregar o balanço.";
            return;
        }

        _balanco = r.Dados;
        var tipo = _balanco.TipoBalanco == TipoBalanco.Consolidado ? "Consolidado" : "Individual";
        lblTitulo.Text = $"Balanço {_balanco.AnoExercicio} · {tipo}";
        lblSubtitulo.Text = $"{_balanco.Contas.Count} contas · planilhado em {_balanco.DataPlanilhamento:dd/MM/yyyy HH:mm}";

        MontarTabela();
        AtualizarTotais();

        // Busca a DRE correspondente (mesma empresa/ano/tipo), se existir
        try
        {
            _dre = await _dreDao.BuscarAtivaAsync(_balanco.EmpresaId, _balanco.AnoExercicio, _balanco.TipoBalanco);
        }
        catch { _dre = null; }
        MontarDre();
        btnAbaDre.Text = _dre is null ? "DRE (não importada)" : "DRE";
    }

    // ─────────────────────────── Abas Balanço / DRE ───────────────────────────

    private void OnVerBalancoClicado(object? sender, EventArgs e)
    {
        layoutContas.IsVisible = true;
        layoutDre.IsVisible = false;
        AtualizarEstiloAbas(balancoAtivo: true);
    }

    private void OnVerDreClicado(object? sender, EventArgs e)
    {
        layoutContas.IsVisible = false;
        layoutDre.IsVisible = true;
        AtualizarEstiloAbas(balancoAtivo: false);
    }

    private void AtualizarEstiloAbas(bool balancoAtivo)
    {
        var ativo = Color.FromArgb("#1F3A60");
        var inativo = Color.FromArgb("#33445A");
        btnAbaBalanco.BackgroundColor = balancoAtivo ? ativo : inativo;
        btnAbaBalanco.TextColor = Colors.White;
        btnAbaDre.BackgroundColor = balancoAtivo ? inativo : ativo;
        btnAbaDre.TextColor = Colors.White;
    }

    private void MontarDre()
    {
        layoutDre.Children.Clear();

        if (_dre is null)
        {
            layoutDre.Children.Add(new Label
            {
                Text = "Este balanço não possui DRE importada.\n\nA DRE é extraída automaticamente quando o PDF importado contém a Demonstração do Resultado do Exercício.",
                FontSize = 14,
                Margin = new Thickness(0, 24),
                HorizontalTextAlignment = TextAlignment.Center
            });
            return;
        }

        layoutDre.Children.Add(new Label
        {
            Text = $"Demonstração do Resultado do Exercício — {_dre.AnoExercicio}",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 8, 0, 12)
        });

        LinhaDre("Receita Líquida", _dre.ReceitaLiquida, destaque: true);
        LinhaDre("Lucro Bruto", _dre.LucroBruto);
        LinhaDre("Resultado Operacional (EBIT)", _dre.ResultadoOperacional);
        LinhaDre("Despesas Financeiras", _dre.DespesasFinanceiras);
        LinhaDre("Lucro Líquido", _dre.LucroLiquido, destaque: true);
    }

    private void LinhaDre(string rotulo, decimal valor, bool destaque = false)
    {
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(200) },
            Padding = new Thickness(12, 9),
            ColumnSpacing = 8
        };
        var bg = new Border
        {
            StrokeThickness = 0,
            Padding = 0,
            Margin = new Thickness(0, 2),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = grid
        };
        bg.SetAppThemeColor(Border.BackgroundColorProperty,
            destaque ? Color.FromArgb("#EEF2F7") : Colors.Transparent,
            destaque ? Color.FromArgb("#243140") : Colors.Transparent);

        var lbl = new Label
        {
            Text = rotulo,
            FontSize = 14,
            FontAttributes = destaque ? FontAttributes.Bold : FontAttributes.None,
            VerticalOptions = LayoutOptions.Center
        };
        lbl.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));
        Grid.SetColumn(lbl, 0);

        var val = new Label
        {
            Text = valor.ToString("N2", _ptBR),
            FontSize = 14,
            FontAttributes = destaque ? FontAttributes.Bold : FontAttributes.None,
            HorizontalTextAlignment = TextAlignment.End,
            VerticalOptions = LayoutOptions.Center
        };
        val.SetAppThemeColor(Label.TextColorProperty,
            valor < 0 ? _corVermelho : Color.FromArgb("#1A1A1A"),
            valor < 0 ? Color.FromArgb("#FF6B6B") : Color.FromArgb("#F0F2F5"));
        Grid.SetColumn(val, 1);

        grid.Children.Add(lbl);
        grid.Children.Add(val);
        layoutDre.Children.Add(bg);
    }

    private void MontarTabela()
    {
        layoutContas.Children.Clear();
        _entries.Clear();
        if (_balanco is null) return;

        // Agrupa as contas por grupo principal, ordenando por código
        var grupos = _balanco.Contas
            .Where(c => c.ContaPadrao is not null)
            .OrderBy(c => c.ContaPadrao!.Ordem)
            .ThenBy(c => c.ContaPadrao!.Codigo)
            .GroupBy(c => c.ContaPadrao!.GrupoPrincipal);

        foreach (var grupo in grupos)
        {
            // Cabeçalho do grupo
            var header = new Border
            {
                Padding = new Thickness(12, 8),
                Margin = new Thickness(0, 12, 0, 4),
                StrokeThickness = 0
            };
            header.SetAppThemeColor(Border.BackgroundColorProperty,
                Color.FromArgb("#EEF2F7"), Color.FromArgb("#243140"));
            header.Content = new Label
            {
                Text = NomeGrupo(grupo.Key),
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            };
            layoutContas.Children.Add(header);

            // Linhas
            foreach (var conta in grupo)
            {
                var cp = conta.ContaPadrao!;
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(90),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(180)
                    },
                    Padding = new Thickness(8, 4),
                    ColumnSpacing = 8
                };

                var lblCod = new Label
                {
                    Text = cp.Codigo,
                    FontFamily = "Consolas",
                    FontSize = 12,
                    VerticalOptions = LayoutOptions.Center
                };
                lblCod.SetAppThemeColor(Label.TextColorProperty,
                    Color.FromArgb("#5A6478"), Color.FromArgb("#A8B0BF"));
                Grid.SetColumn(lblCod, 0);

                var lblDesc = new Label
                {
                    Text = cp.Descricao,
                    FontSize = 13,
                    VerticalOptions = LayoutOptions.Center,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                lblDesc.SetAppThemeColor(Label.TextColorProperty,
                    Color.FromArgb("#1A1A1A"), Color.FromArgb("#F0F2F5"));
                Grid.SetColumn(lblDesc, 1);

                var border = new Border
                {
                    StrokeThickness = 1,
                    HeightRequest = 32,
                    Padding = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 }
                };
                border.SetAppThemeColor(Border.StrokeProperty,
                    Color.FromArgb("#C5CCD6"), Color.FromArgb("#3A4554"));
                border.SetAppThemeColor(Border.BackgroundColorProperty,
                    Colors.White, Color.FromArgb("#13202B"));

                var entry = new Entry
                {
                    Text = conta.Valor == 0 ? string.Empty : conta.Valor.ToString("N2", _ptBR),
                    Keyboard = Keyboard.Numeric,
                    HorizontalTextAlignment = TextAlignment.End,
                    FontSize = 13,
                    BackgroundColor = Colors.Transparent,
                    Margin = new Thickness(8, 0)
                };
                entry.Unfocused += (s, e) => AtualizarTotais();
                border.Content = entry;
                Grid.SetColumn(border, 2);

                _entries[cp.Id] = entry;

                grid.Children.Add(lblCod);
                grid.Children.Add(lblDesc);
                grid.Children.Add(border);
                layoutContas.Children.Add(grid);
            }
        }
    }

    private void AtualizarTotais()
    {
        if (_balanco is null) return;

        decimal ativo = 0, passivoMaisPl = 0;
        foreach (var conta in _balanco.Contas)
        {
            var cp = conta.ContaPadrao;
            if (cp is null) continue;
            if (!_entries.TryGetValue(cp.Id, out var entry)) continue;

            var v = CelulaPlanilhamento.TentarParsear(entry.Text) ?? 0m;
            if (cp.Codigo.StartsWith("1", StringComparison.Ordinal)) ativo += v;
            else if (cp.Codigo.StartsWith("2", StringComparison.Ordinal)) passivoMaisPl += v;
        }

        var dif = ativo - passivoMaisPl;
        var fecha = Math.Abs(dif) <= 1.00m;

        lblTotais.Text = $"Ativo: R$ {ativo.ToString("N2", _ptBR)}    " +
                         $"Passivo + PL: R$ {passivoMaisPl.ToString("N2", _ptBR)}";
        lblStatus.Text = fecha ? "Balanço fecha" : $"Diferença de R$ {dif.ToString("N2", _ptBR)}";
        lblStatus.TextColor = fecha ? _corVerde : _corVermelho;
    }

    private async void OnSalvarClicado(object? sender, EventArgs e)
    {
        if (_balanco is null) return;

        // Reconstrói as contas a partir dos Entries (só as com valor != 0)
        var novasContas = new List<ContaBalanco>();
        foreach (var conta in _balanco.Contas)
        {
            var cp = conta.ContaPadrao;
            if (cp is null) continue;
            if (!_entries.TryGetValue(cp.Id, out var entry)) continue;

            var v = CelulaPlanilhamento.TentarParsear(entry.Text) ?? 0m;
            if (v != 0)
            {
                novasContas.Add(new ContaBalanco
                {
                    ContaPadraoId = cp.Id,
                    Valor = v,
                    DescricaoOriginal = conta.DescricaoOriginal
                });
            }
        }

        if (novasContas.Count == 0)
        {
            await DisplayAlert("Atenção", "O balanço ficaria sem nenhuma conta. Preencha ao menos um valor.", "OK");
            return;
        }

        _balanco.Contas = novasContas;
        var r = await _balancoController.AtualizarAsync(_balanco);

        if (r.Sucesso)
        {
            await DisplayAlert("Salvo", "Alterações salvas com sucesso.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Erro", r.Mensagem, "OK");
        }
    }

    private async void OnExportarClicado(object? sender, EventArgs e)
    {
        if (_balanco is null) return;

        var formato = await DisplayActionSheet(
            "Exportar balanço como:", "Cancelar", null, "PDF", "Excel", "JSON", "Apresentação PPT (Mock)");
        if (string.IsNullOrEmpty(formato) || formato == "Cancelar") return;

        try
        {
            btnExportar.IsEnabled = false;
            btnExportar.Text = "Exportando...";

            var r = formato switch
            {
                "PDF" => await _exportacao.ExportarPdfAsync(_balancoId),
                "Excel" => await _exportacao.ExportarExcelAsync(_balancoId),
                "JSON" => await _exportacao.ExportarJsonAsync(_balancoId),
                "Apresentação PPT (Mock)" => await _exportacao.ExportarPptAsync(_balancoId),
                _ => ResultadoOperacao<string>.Falha("Formato inválido.")
            };

            if (!r.Sucesso || string.IsNullOrEmpty(r.Dados))
            {
                await DisplayAlert("Erro", r.Mensagem, "OK");
                return;
            }

            var caminho = r.Dados;
            var abrir = await DisplayAlert("Exportado",
                $"Arquivo salvo em:\n{caminho}", "Abrir", "OK");

            if (abrir)
            {
                try
                {
                    await Launcher.Default.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(caminho)
                    });
                }
                catch
                {
                    // Se não conseguir abrir o arquivo, tenta abrir a pasta
                    await DisplayAlert("Arquivo salvo", $"O arquivo está em:\n{caminho}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", $"Falha ao exportar: {ex.Message}", "OK");
        }
        finally
        {
            btnExportar.IsEnabled = true;
            btnExportar.Text = "Exportar";
        }
    }

    private async void OnExcluirClicado(object? sender, EventArgs e)
    {
        var ok = await DisplayAlert("Excluir balanço",
            "Tem certeza que deseja excluir este balanço? Esta ação não pode ser desfeita.",
            "Excluir", "Cancelar");
        if (!ok) return;

        var r = await _balancoController.ExcluirAsync(_balancoId);
        if (r.Sucesso)
        {
            await DisplayAlert("Excluído", "Balanço excluído.", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Erro", r.Mensagem, "OK");
        }
    }

    private async void OnFecharClicado(object? sender, EventArgs e)
        => await Navigation.PopAsync();

    private static string NomeGrupo(GrupoContaPrincipal g) => g switch
    {
        GrupoContaPrincipal.Ativo => "ATIVO",
        GrupoContaPrincipal.Passivo => "PASSIVO",
        GrupoContaPrincipal.PatrimonioLiquido => "PATRIMÔNIO LÍQUIDO",
        _ => g.ToString()
    };
}
