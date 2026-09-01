using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class BalancosEmpresaPage : ContentPage
{
    private readonly BalancosEmpresaViewModel _viewModel;

    public BalancosEmpresaPage(BalancosEmpresaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void Inicializar(int empresaId, string empresaNome)
    {
        _viewModel.Inicializar(empresaId, empresaNome);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarAsync();
    }
}
