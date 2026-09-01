using BalancoPatrimonial.App.Controllers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BalancoPatrimonial.App.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ILoginController _controller;

    [ObservableProperty]
    private string _login = string.Empty;

    [ObservableProperty]
    private string _senha = string.Empty;

    [ObservableProperty]
    private string _erroMensagem = string.Empty;

    [ObservableProperty]
    private bool _temErro;

    public LoginViewModel(ILoginController controller)
    {
        _controller = controller;
    }

    [RelayCommand]
    public async Task EntrarAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        TemErro = false;
        ErroMensagem = string.Empty;

        try
        {
            var resultado = await _controller.AutenticarAsync(Login, Senha);

            if (resultado.Sucesso)
            {
                Login = string.Empty;
                Senha = string.Empty;
                Application.Current!.MainPage = new AppShell();
            }
            else
            {
                ErroMensagem = resultado.Mensagem;
                TemErro = true;
            }
        }
        catch (Exception ex)
        {
            ErroMensagem = "Erro inesperado: " + ex.Message;
            TemErro = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}