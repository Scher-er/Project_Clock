using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Views.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class BalancosEmpresaViewModel : BaseViewModel
{
    private readonly IBalancoController _balancoController;
    private int _empresaId;
    private string _empresaNome = string.Empty;

    [ObservableProperty]
    private string _subtitulo = string.Empty;

    private bool _temBalancos;
    public bool TemBalancos
    {
        get => _temBalancos;
        set
        {
            if (SetProperty(ref _temBalancos, value))
            {
                OnPropertyChanged(nameof(NaoTemBalancos));
            }
        }
    }

    public bool NaoTemBalancos => !TemBalancos;

    public ObservableCollection<BalancoResumoItem> Balancos { get; } = new();

    public BalancosEmpresaViewModel(IBalancoController balancoController)
    {
        _balancoController = balancoController;
    }

    public void Inicializar(int empresaId, string empresaNome)
    {
        _empresaId = empresaId;
        _empresaNome = empresaNome;
        Title = $"Balanços — {empresaNome}";
    }

    public async Task CarregarAsync()
    {
        IsBusy = true;
        try
        {
            var r = await _balancoController.ListarPorEmpresaAsync(_empresaId);

            if (!r.Sucesso || r.Dados is null)
            {
                Subtitulo = "Erro ao carregar balanços.";
                TemBalancos = false;
                return;
            }

            var balancos = r.Dados.ToList();
            if (balancos.Count == 0)
            {
                Subtitulo = $"{_empresaNome} ainda não tem balanços armazenados.";
                TemBalancos = false;
                return;
            }

            Balancos.Clear();
            var itens = balancos
                .OrderByDescending(b => b.AnoExercicio)
                .ThenBy(b => b.TipoBalanco)
                .Select(b => new BalancoResumoItem(b))
                .ToList();

            foreach(var item in itens) Balancos.Add(item);

            Subtitulo = $"{itens.Count} balanço(s) armazenado(s).";
            TemBalancos = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FecharAsync()
    {
        await Application.Current!.MainPage!.Navigation.PopAsync();
    }

    [RelayCommand]
    private async Task VerDetalhesAsync(int balancoId)
    {
        var nav = Application.Current!.MainPage!.Navigation;

        var detalhePage = App.Services.GetRequiredService<BalancoPatrimonial.App.Views.DetalheBalancoPage>();
        detalhePage.Inicializar(balancoId);

        await nav.PushAsync(detalhePage);
    }

}