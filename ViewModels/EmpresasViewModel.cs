using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views;
using BalancoPatrimonial.App.Views.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class EmpresasViewModel : BaseViewModel
{
    private readonly IEmpresaController _controller;
    private readonly IConfiguracaoLocalService _config;
    private readonly IExportacaoController _exportController;
    private readonly ISessaoUsuario _sessao;

    public ObservableCollection<EmpresaListItem> Items { get; } = new();

    [ObservableProperty]
    private string _textoPesquisa = string.Empty;

    [ObservableProperty]
    private string _subtitulo = "Carregando...";

    [ObservableProperty]
    private string _mensagemVazia = string.Empty;

    [ObservableProperty]
    private bool _temMensagemVazia;

    [ObservableProperty]
    private bool _listaVisivel;

    [ObservableProperty]
    private bool _isRefreshing;

    public EmpresasViewModel(IEmpresaController controller, IConfiguracaoLocalService config,
        IExportacaoController exportController, ISessaoUsuario sessao)
    {
        _controller = controller;
        _config = config;
        _exportController = exportController;
        _sessao = sessao;
        Title = "Empresas";
    }

    [RelayCommand]
    public async Task CarregarInicialAsync()
    {
        var ultimoFiltro = await _config.ObterUltimaPesquisaAsync();
        if (!string.IsNullOrEmpty(ultimoFiltro))
        {
            TextoPesquisa = ultimoFiltro;
        }
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task RecarregarAsync()
    {
        await CarregarAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    public async Task PesquisarAsync()
    {
        await _config.DefinirUltimaPesquisaAsync(TextoPesquisa ?? string.Empty);
        await CarregarAsync();
    }

    [RelayCommand]
    public async Task NovaEmpresaAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(CadastroEmpresaPage)}?id=0");
    }

    [RelayCommand]
    public async Task VerBalancosAsync(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);
        var nome = item?.RazaoSocial ?? "Empresa";
        
        var page = App.Services.GetRequiredService<BalancoPatrimonial.App.Views.BalancosEmpresaPage>();
        page.Inicializar(id, nome);
        await Application.Current!.MainPage!.Navigation.PushAsync(page);
    }

    [RelayCommand]
    public async Task EditarEmpresaAsync(int id)
    {
        await Shell.Current.GoToAsync($"{nameof(CadastroEmpresaPage)}?id={id}");
    }

    [RelayCommand]
    public async Task ExcluirAsync(int id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        var confirmar = await Application.Current!.MainPage!.DisplayAlert(
            "Confirmar exclusão",
            $"Excluir '{item.RazaoSocial}'?\n\nIsso também removerá os balanços vinculados. Esta ação é irreversível.",
            "Excluir",
            "Cancelar");

        if (!confirmar) return;

        var resultado = await _controller.ExcluirAsync(id);
        if (resultado.Sucesso)
        {
            Items.Remove(item);
            await Application.Current.MainPage.DisplayAlert("Pronto", "Empresa excluída.", "OK");
            AtualizarSubtitulo();
        }
        else
        {
            await Application.Current.MainPage.DisplayAlert("Erro", resultado.Mensagem, "OK");
        }
    }

    [RelayCommand]
    public async Task ImportarJsonAsync()
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
            if (arquivo is null) return;

            var usuarioId = _sessao.UsuarioAtual?.Id ?? 1;
            var r = await _exportController.ImportarJsonAsync(arquivo.FullPath, usuarioId);

            if (r.Sucesso)
            {
                await Application.Current!.MainPage!.DisplayAlert("Importação concluída", r.Dados, "OK");
                await CarregarAsync();
            }
            else
            {
                var detalhe = r.Erros.Count > 0 ? "\n\n• " + string.Join("\n• ", r.Erros) : "";
                await Application.Current!.MainPage!.DisplayAlert("Não foi possível importar", r.Mensagem + detalhe, "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Erro", "Falha ao importar: " + ex.Message, "OK");
        }
    }

    private async Task CarregarAsync()
    {
        try
        {
            IsBusy = true;
            var termo = TextoPesquisa?.Trim() ?? string.Empty;
            var resultado = string.IsNullOrEmpty(termo)
                ? await _controller.ListarAsync()
                : await _controller.PesquisarAsync(termo);

            if (!resultado.Sucesso)
            {
                MostrarVazio(resultado.Mensagem);
                return;
            }

            Items.Clear();
            foreach (var e in resultado.Dados!)
            {
                Items.Add(EmpresaListItem.De(e));
            }

            if (Items.Count == 0)
            {
                MostrarVazio(string.IsNullOrEmpty(termo)
                    ? "Nenhuma empresa cadastrada."
                    : $"Nenhum resultado pra '{termo}'.");
            }
            else
            {
                TemMensagemVazia = false;
                ListaVisivel = true;
            }

            AtualizarSubtitulo();
        }
        catch (Exception ex)
        {
            MostrarVazio($"Erro ao carregar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void MostrarVazio(string mensagem)
    {
        MensagemVazia = mensagem;
        TemMensagemVazia = true;
        ListaVisivel = false;
        AtualizarSubtitulo();
    }

    private void AtualizarSubtitulo()
    {
        var total = Items.Count;
        Subtitulo = total switch
        {
            0 => "Nenhuma empresa listada.",
            1 => "1 empresa.",
            _ => $"{total} empresas."
        };
    }
}