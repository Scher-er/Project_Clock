using System.Globalization;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Tela de cadastro/edição de Empresa. Recebe id via query string do Shell
/// (rota: "//empresas/cadastroempresa?id=N", onde id=0 significa "novo").
///
/// Não usa MVVM toolkit pra ficar mais didático — manipula controles
/// no code-behind direto, exibindo claramente o fluxo:
///   View → Controller (EmpresaController) → Service (EmpresaService) → DAO (MySqlEmpresaDao)
/// </summary>
[QueryProperty(nameof(EmpresaId), "id")]
public partial class CadastroEmpresaPage : ContentPage
{
    private readonly IEmpresaController _controller;
    private readonly IExportacaoController _exportController;
    private readonly IListagensService _listagens;
    private int _id;
    private Empresa? _empresaCarregada;

    public int EmpresaId
    {
        get => _id;
        set { _id = value; _ = CarregarAsync(); }
    }

    public CadastroEmpresaPage(
        IEmpresaController controller,
        IExportacaoController exportController,
        IListagensService listagens)
    {
        InitializeComponent();
        _controller = controller;
        _exportController = exportController;
        _listagens = listagens;
        PopularDropdownsEstaticos();
    }

    private void PopularDropdownsEstaticos()
    {
        // Tipos de empresa (enum)
        pkTipo.ItemsSource = new[]
        {
            new TipoEmpresaWrapper(TipoEmpresa.SaAberta,   "S.A. de capital aberto"),
            new TipoEmpresaWrapper(TipoEmpresa.SaFechada,  "S.A. de capital fechado"),
            new TipoEmpresaWrapper(TipoEmpresa.Ltda,       "LTDA"),
            new TipoEmpresaWrapper(TipoEmpresa.Eireli,     "EIRELI"),
            new TipoEmpresaWrapper(TipoEmpresa.Mei,        "MEI"),
            new TipoEmpresaWrapper(TipoEmpresa.Outro,      "Outro"),
        };
        pkTipo.SelectedIndex = 0;

        // Ratings padrão S&P
        pkRating.ItemsSource = new[]
        {
            "(sem rating)", "AAA", "AA+", "AA", "AA-",
            "A+", "A", "A-", "BBB+", "BBB", "BBB-",
            "BB+", "BB", "BB-", "B+", "B", "B-",
            "CCC", "CC", "C", "D"
        };
        pkRating.SelectedIndex = 0;
    }

    private async Task CarregarAsync()
    {
        // Popula grupos e setores (sempre, independente de novo/editar)
        try
        {
            var grupos = (await _listagens.ListarGruposEconomicosAsync()).ToList();
            grupos.Insert(0, new GrupoEconomico { Id = 0, Nome = "(sem grupo)" });
            pkGrupo.ItemsSource = grupos;
            pkGrupo.SelectedIndex = 0;

            var setores = (await _listagens.ListarSetoresAsync()).ToList();
            lvSetores.ItemsSource = setores;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro",
                $"Não foi possível carregar grupos/setores: {ex.Message}", "OK");
        }

        if (_id <= 0)
        {
            lblTitulo.Text = "Nova Empresa";
            lblSubtitulo.Text = "Preencha os dados da empresa.";
            btnSalvar.Text = "Cadastrar";
            return;
        }

        // Modo edição — carrega dados existentes
        lblTitulo.Text = "Editar Empresa";
        btnSalvar.Text = "Salvar alterações";

        var resultado = await _controller.BuscarCompletaAsync(_id);
        if (!resultado.Sucesso)
        {
            await DisplayAlert("Erro", resultado.Mensagem, "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _empresaCarregada = resultado.Dados;
        PreencherCampos(_empresaCarregada!);
        PreencherBalancos(_empresaCarregada!);
    }

    private void PreencherBalancos(Empresa e)
    {
        if (e.Balancos.Count == 0)
        {
            cardBalancos.IsVisible = false;
            return;
        }

        cardBalancos.IsVisible = true;
        lblQtdBalancos.Text = e.Balancos.Count switch
        {
            1 => "1 balanço",
            _ => $"{e.Balancos.Count} balanços"
        };

        lvBalancos.ItemsSource = e.Balancos
            .OrderByDescending(b => b.AnoExercicio)
            .ThenBy(b => b.TipoBalanco)
            .Select(BalancoListItem.De)
            .ToList();
    }

    // ───── Exportações ─────

    private async void OnExportarExcelClicado(object? sender, EventArgs e)
        => await ExportarAsync(sender, formato: "Excel");

    private async void OnExportarPdfClicado(object? sender, EventArgs e)
        => await ExportarAsync(sender, formato: "PDF");

    private async void OnExportarJsonClicado(object? sender, EventArgs e)
        => await ExportarAsync(sender, formato: "JSON");

    private async Task ExportarAsync(object? sender, string formato)
    {
        if (sender is not Button btn || btn.CommandParameter is not int balancoId)
            return;

        btn.IsEnabled = false;
        var textoOriginal = btn.Text;
        btn.Text = "...";

        try
        {
            var resultado = formato switch
            {
                "Excel" => await _exportController.ExportarExcelAsync(balancoId),
                "PDF"   => await _exportController.ExportarPdfAsync(balancoId),
                _       => await _exportController.ExportarJsonAsync(balancoId)
            };

            if (!resultado.Sucesso || resultado.Dados is null)
            {
                await DisplayAlert("Erro", resultado.Mensagem, "OK");
                return;
            }

            var caminho = resultado.Dados;
            var nomeArquivo = Path.GetFileName(caminho);

            // Oferece compartilhar (abre share sheet do SO — funciona em Win/Mac/iOS/Android)
            var compartilhar = await DisplayAlert(
                $"{formato} gerado",
                $"Arquivo salvo em:\n\n{caminho}\n\nDeseja compartilhar/abrir?",
                "Compartilhar", "Fechar");

            if (compartilhar)
            {
                try
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = nomeArquivo,
                        File = new ShareFile(caminho)
                    });
                }
                catch (Exception ex)
                {
                    // Em alguns ambientes (CI, emuladores) Share.Default pode não estar disponível
                    await DisplayAlert("Aviso",
                        $"Não foi possível abrir o compartilhamento, mas o arquivo está em:\n{caminho}\n\nErro: {ex.Message}",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro inesperado", ex.Message, "OK");
        }
        finally
        {
            btn.Text = textoOriginal;
            btn.IsEnabled = true;
        }
    }

    private void PreencherCampos(Empresa e)
    {
        lblSubtitulo.Text = $"Editando '{e.RazaoSocial}'.";
        txtCnpj.Text = FormatarCnpj(e.Cnpj);
        txtRazaoSocial.Text = e.RazaoSocial;
        txtNomeFantasia.Text = e.NomeFantasia ?? string.Empty;
        txtUf.Text = e.UfAtuacao ?? string.Empty;
        txtLocal.Text = e.LocalAtuacao ?? string.Empty;
        txtLimite.Text = e.LimiteCredito?.ToString("N2", new CultureInfo("pt-BR")) ?? string.Empty;

        // Seleciona tipo
        if (pkTipo.ItemsSource is IEnumerable<TipoEmpresaWrapper> tipos)
        {
            var i = tipos.ToList().FindIndex(t => t.Valor == e.TipoEmpresa);
            pkTipo.SelectedIndex = i >= 0 ? i : 0;
        }

        // Seleciona rating
        if (pkRating.ItemsSource is IEnumerable<string> ratings && !string.IsNullOrEmpty(e.Rating))
        {
            var i = ratings.ToList().IndexOf(e.Rating);
            pkRating.SelectedIndex = i >= 0 ? i : 0;
        }

        // Seleciona grupo
        if (e.GrupoEconomicoId.HasValue && pkGrupo.ItemsSource is IEnumerable<GrupoEconomico> grupos)
        {
            var i = grupos.ToList().FindIndex(g => g.Id == e.GrupoEconomicoId.Value);
            pkGrupo.SelectedIndex = i >= 0 ? i : 0;
        }

        // Marca setores
        if (lvSetores.ItemsSource is IEnumerable<SetorAtividade> setoresLista)
        {
            var idsVinculados = e.Setores.Select(s => s.Id).ToHashSet();
            foreach (var s in setoresLista)
            {
                if (idsVinculados.Contains(s.Id))
                {
                    lvSetores.SelectedItems.Add(s);
                }
            }
            AtualizarContagemSetores();
        }
    }

    private void OnSetorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        AtualizarContagemSetores();
    }

    private void AtualizarContagemSetores()
    {
        var n = lvSetores.SelectedItems?.Count ?? 0;
        lblContagemSetores.Text = n switch
        {
            0 => "Nenhum selecionado",
            1 => "1 selecionado (principal)",
            _ => $"{n} selecionados"
        };
    }

    private async void OnCancelarClicado(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSalvarClicado(object? sender, EventArgs e)
    {
        btnSalvar.IsEnabled = false;
        try
        {
            var empresa = ConstruirEmpresa();
            var setoresIds = (lvSetores.SelectedItems ?? new List<object>())
                .OfType<SetorAtividade>()
                .Select(s => s.Id)
                .ToList();

            if (_id <= 0)
            {
                var r = await _controller.CriarComSetoresAsync(empresa, setoresIds);
                if (r.Sucesso)
                {
                    await DisplayAlert("Pronto", r.Mensagem, "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await DisplayAlert("Erro", MontarMensagem(r.Mensagem, r.Erros), "OK");
                }
            }
            else
            {
                empresa.Id = _id;
                var r = await _controller.AtualizarAsync(empresa);
                if (r.Sucesso)
                {
                    await DisplayAlert("Pronto", "Empresa atualizada.", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await DisplayAlert("Erro", MontarMensagem(r.Mensagem, r.Erros), "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro inesperado", ex.Message, "OK");
        }
        finally
        {
            btnSalvar.IsEnabled = true;
        }
    }

    private Empresa ConstruirEmpresa()
    {
        var tipo = pkTipo.SelectedItem is TipoEmpresaWrapper t ? t.Valor : TipoEmpresa.Ltda;
        var rating = pkRating.SelectedItem as string;
        if (rating == "(sem rating)") rating = null;

        int? grupoId = null;
        if (pkGrupo.SelectedItem is GrupoEconomico g && g.Id > 0) grupoId = g.Id;

        decimal? limite = null;
        if (!string.IsNullOrWhiteSpace(txtLimite.Text)
            && decimal.TryParse(txtLimite.Text, NumberStyles.Any, new CultureInfo("pt-BR"), out var lim))
        {
            limite = lim;
        }

        return new Empresa
        {
            Cnpj = txtCnpj.Text ?? string.Empty,  // EmpresaService faz o LimparCnpj
            RazaoSocial = txtRazaoSocial.Text?.Trim() ?? string.Empty,
            NomeFantasia = string.IsNullOrWhiteSpace(txtNomeFantasia.Text) ? null : txtNomeFantasia.Text.Trim(),
            UfAtuacao = string.IsNullOrWhiteSpace(txtUf.Text) ? null : txtUf.Text.Trim().ToUpper(),
            LocalAtuacao = string.IsNullOrWhiteSpace(txtLocal.Text) ? null : txtLocal.Text.Trim(),
            TipoEmpresa = tipo,
            Rating = rating,
            LimiteCredito = limite,
            GrupoEconomicoId = grupoId,
            Ativo = true
        };
    }

    private static string MontarMensagem(string mensagem, IReadOnlyList<string> erros)
    {
        if (erros.Count == 0) return mensagem;
        return $"{mensagem}\n\n• {string.Join("\n• ", erros)}";
    }

    private static string FormatarCnpj(string cnpj)
    {
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14) return cnpj;
        return $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..14]}";
    }
}

/// <summary>Wrapper pra usar enum em Picker com label customizada.</summary>
public record TipoEmpresaWrapper(TipoEmpresa Valor, string Nome)
{
    public override string ToString() => Nome;
}
