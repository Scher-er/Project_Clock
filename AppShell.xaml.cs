using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views;

namespace BalancoPatrimonial.App;

public partial class AppShell : Shell
{
    private readonly ISessaoUsuario _sessao;

    public AppShell()
    {
        InitializeComponent();

        _sessao = App.Services.GetRequiredService<ISessaoUsuario>();

        // Mostra o nome do usuário logado no cabeçalho do sidebar
        if (_sessao.Autenticado && _sessao.UsuarioAtual is not null)
        {
            lblUsuarioLogado.Text = $"👤 {_sessao.UsuarioAtual.Nome}";
        }

        // Páginas que NÃO aparecem no flyout — abertas via Shell.GoToAsync
        Routing.RegisterRoute(nameof(CadastroEmpresaPage), typeof(CadastroEmpresaPage));
    }

    /// <summary>
    /// Encerra a sessão e volta pra LoginPage.
    /// </summary>
    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        bool confirma = await DisplayAlert(
            "Sair",
            "Deseja realmente encerrar a sessão?",
            "Sair", "Cancelar");

        if (!confirma) return;

        var auth = App.Services.GetRequiredService<IAuthService>();
        auth.EncerrarSessao();

        var loginPage = App.Services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#1F3A60"),
            BarTextColor = Colors.White
        };
    }
}
