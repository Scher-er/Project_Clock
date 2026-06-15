using BalancoPatrimonial.App.Controllers;

namespace BalancoPatrimonial.App.Views;

public partial class LoginPage : ContentPage
{
    private readonly ILoginController _controller;

    public LoginPage(ILoginController controller)
    {
        InitializeComponent();
        _controller = controller;
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        // Desabilita UI e mostra loading
        btnEntrar.IsEnabled = false;
        loading.IsRunning = true;
        loading.IsVisible = true;
        lblErro.IsVisible = false;

        try
        {
            var resultado = await _controller.AutenticarAsync(
                txtLogin.Text ?? string.Empty,
                txtSenha.Text ?? string.Empty);

            if (resultado.Sucesso)
            {
                // Limpa campos antes de trocar de página
                txtLogin.Text = string.Empty;
                txtSenha.Text = string.Empty;

                // Substitui a MainPage pelo AppShell (entra no app)
                Application.Current!.MainPage = new AppShell();
            }
            else
            {
                lblErro.Text = resultado.Mensagem;
                lblErro.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            lblErro.Text = "Erro inesperado: " + ex.Message;
            lblErro.IsVisible = true;
        }
        finally
        {
            btnEntrar.IsEnabled = true;
            loading.IsRunning = false;
            loading.IsVisible = false;
        }
    }
}
