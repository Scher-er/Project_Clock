using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class PlanoContasPage : ContentPage
{
    private readonly PlanoContasViewModel _viewModel;

    public PlanoContasPage(PlanoContasViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.CarregarCommand.ExecuteAsync(null);
    }
}
