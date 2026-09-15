using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class EmpresasPage : ContentPage
{
    private readonly EmpresasViewModel _viewModel;

    public EmpresasPage(EmpresasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await _viewModel.CarregarInicialAsync(); } catch { }
    }
}
