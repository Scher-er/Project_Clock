using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views.Items;

namespace BalancoPatrimonial.App.Views;

/// <summary>
/// Tela de listagem de empresas. Fluxo:
///   1) OnAppearing → recarrega lista
///   2) Usuário digita na pesquisa → OnPesquisarClicado dispara
///   3) Toca num item → navega pra CadastroEmpresaPage com o id
///   4) Swipe esquerdo + Excluir → confirma e exclui
///   5) Botão "+ Nova" → CadastroEmpresaPage com id=0 (novo)
/// </summary>
public partial class EmpresasPage : ContentPage
{
    private readonly IEmpresaController _controller;
    private readonly IConfiguracaoLocalService _config;
    private readonly IExportacaoController _exportController;
    private readonly ISessaoUsuario _sessao;
    private readonly ObservableCollection<EmpresaListItem> _items = new();

    public EmpresasPage(IEmpresaController controller, IConfiguracaoLocalService config,
        IExportacaoController exportController, ISessaoUsuario sessao)
    {
        InitializeComponent();
        _controller = controller;
        _config = config;
        _exportController = exportController;
        _sessao = sessao;

        lvEmpresas.ItemsSource = _items;
    }

    private async void OnImportarJsonClicado(object? sender, EventArgs e)
    {
        try
        {
            var tipoJson = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".json" },
                [DevicePlatform.macOS] = new[] { "json" }
            });
            var arquivo = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecione um arquivo .json de balanço",
                FileTypes = tipoJson
            });
            if (arquivo is null) return; // cancelado

            var usuarioId = _sessao.UsuarioAtual?.Id ?? 1;
            var r = await _exportController.ImportarJsonAsync(arquivo.FullPath, usuarioId);

            if (r.Sucesso)
            {
                await DisplayAlert("Importação concluída", r.Dados, "OK");
                OnAppearing(); // recarrega a lista
            }
            else
            {
                var detalhe = r.Erros.Count > 0 ? "\n\n• " + string.Join("\n• ", r.Erros) : "";
                await DisplayAlert("Não foi possível importar", r.Mensagem + detalhe, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", "Falha ao importar: " + ex.Message, "OK");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Restaura último filtro (preferência do usuário guardada no SQLite)
        var ultimoFiltro = await _config.ObterUltimaPesquisaAsync();
        if (!string.IsNullOrEmpty(ultimoFiltro))
        {
            txtPesquisa.Text = ultimoFiltro;
        }

        await CarregarAsync();
    }

    private async void OnRecarregar(object? sender, EventArgs e)
    {
        await CarregarAsync();
        refreshView.IsRefreshing = false;
    }

    private async void OnPesquisarClicado(object? sender, EventArgs e)
    {
        await _config.DefinirUltimaPesquisaAsync(txtPesquisa.Text ?? string.Empty);
        await CarregarAsync();
    }

    private async void OnNovaEmpresaClicado(object? sender, EventArgs e)
    {
        // Navega pra cadastro com id=0 (sinaliza "criar nova")
        await Shell.Current.GoToAsync($"{nameof(CadastroEmpresaPage)}?id=0");
    }

    private async void OnEmpresaTocada(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is int id)
        {
            await Shell.Current.GoToAsync($"{nameof(CadastroEmpresaPage)}?id={id}");
        }
    }

    private async void OnExcluirInvocado(object? sender, EventArgs e)
    {
        if (sender is not SwipeItem swipe || swipe.CommandParameter is not int id) return;

        var item = _items.FirstOrDefault(x => x.Id == id);
        var nome = item?.RazaoSocial ?? "esta empresa";

        var confirmar = await DisplayAlert(
            "Confirmar exclusão",
            $"Excluir '{nome}'?\n\nIsso também removerá os balanços vinculados. Esta ação é irreversível.",
            "Excluir",
            "Cancelar");

        if (!confirmar) return;

        var resultado = await _controller.ExcluirAsync(id);
        if (resultado.Sucesso)
        {
            _items.Remove(item!);
            await DisplayAlert("Pronto", "Empresa excluída.", "OK");
            AtualizarSubtitulo();
        }
        else
        {
            await DisplayAlert("Erro", resultado.Mensagem, "OK");
        }
    }

    private async Task CarregarAsync()
    {
        try
        {
            var termo = txtPesquisa.Text?.Trim() ?? string.Empty;
            var resultado = string.IsNullOrEmpty(termo)
                ? await _controller.ListarAsync()
                : await _controller.PesquisarAsync(termo);

            if (!resultado.Sucesso)
            {
                MostrarVazio(resultado.Mensagem);
                return;
            }

            _items.Clear();
            foreach (var e in resultado.Dados!)
            {
                _items.Add(EmpresaListItem.De(e));
            }

            if (_items.Count == 0)
            {
                MostrarVazio(string.IsNullOrEmpty(termo)
                    ? "Nenhuma empresa cadastrada."
                    : $"Nenhum resultado pra '{termo}'.");
            }
            else
            {
                painelVazio.IsVisible = false;
                lvEmpresas.IsVisible = true;
            }

            AtualizarSubtitulo();
        }
        catch (Exception ex)
        {
            MostrarVazio($"Erro ao carregar: {ex.Message}");
        }
    }

    private async void OnVerBalancosClicado(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not int id) return;

        var item = _items.FirstOrDefault(x => x.Id == id);
        var nome = item?.RazaoSocial ?? "Empresa";

        // Abre a máscara de visualização dos balanços como página modal.
        // A página é resolvida via DI pra receber o IBalancoController.
        var page = App.Services.GetRequiredService<BalancosEmpresaPage>();
        page.Inicializar(id, nome);
        await Navigation.PushAsync(page);
    }

    private void MostrarVazio(string mensagem)
    {
        lblMensagemVazia.Text = mensagem;
        painelVazio.IsVisible = true;
        lvEmpresas.IsVisible = false;
        AtualizarSubtitulo();
    }

    private void AtualizarSubtitulo()
    {
        var total = _items.Count;
        lblSubtitulo.Text = total switch
        {
            0 => "Nenhuma empresa listada.",
            1 => "1 empresa.",
            _ => $"{total} empresas."
        };
    }
}
