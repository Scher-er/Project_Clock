using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class CadastroEmpresaPage : ContentPage
{
    public CadastroEmpresaPage(CadastroEmpresaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
