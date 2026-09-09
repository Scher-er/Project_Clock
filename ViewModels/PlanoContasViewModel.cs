using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class PlanoContasViewModel : BaseViewModel
{
    private readonly IListagensService _listagens;

    public ObservableCollection<ContaPadrao> Contas { get; } = new();

    public PlanoContasViewModel(IListagensService listagens)
    {
        _listagens = listagens;
        Title = "Plano de Contas Dinâmico";
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        IsBusy = true;
        try
        {
            var contas = await _listagens.ListarContasPadraoAsync();
            Contas.Clear();
            foreach (var c in contas)
            {
                Contas.Add(c);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
