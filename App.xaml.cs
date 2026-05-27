using BalancoPatrimonial.App.Views;

namespace BalancoPatrimonial.App;

public partial class App : Application
{
    /// <summary>
    /// Provider estático para resolver dependências fora do ciclo de DI normal
    /// (ex: trocar de página dinamicamente após login).
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;

        // Inicia na LoginPage. Após autenticar, a própria LoginPage troca pra AppShell.
        var loginPage = services.GetRequiredService<LoginPage>();
        MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#1F3A60"),
            BarTextColor = Colors.White
        };
    }
}
