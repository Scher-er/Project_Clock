using System.Collections.ObjectModel;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IEmpresaService _empresaService;

    [ObservableProperty] private int _totalEmpresas;
    [ObservableProperty] private int _totalBalancos;
    [ObservableProperty] private string _ultimaAnalise = "Nenhuma";

    public ObservableCollection<Empresa> UltimasEmpresas { get; } = new();

    public DashboardViewModel(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
        Title = "Dashboard Principal";
    }

    [RelayCommand]
    public async Task CarregarDadosAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _empresaService.ListarAsync();
            if (res.Sucesso && res.Dados is not null)
            {
                var lista = res.Dados.ToList();
                TotalEmpresas = lista.Count;
                TotalBalancos = lista.Sum(e => e.Balancos?.Count ?? 0);
                
                UltimasEmpresas.Clear();
                foreach (var e in lista.OrderByDescending(e => e.Id).Take(5))
                {
                    UltimasEmpresas.Add(e);
                }

                if (UltimasEmpresas.Any())
                {
                    UltimaAnalise = UltimasEmpresas.First().RazaoSocial;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
