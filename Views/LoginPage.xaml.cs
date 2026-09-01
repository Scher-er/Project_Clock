using BalancoPatrimonial.App.ViewModels;

namespace BalancoPatrimonial.App.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
