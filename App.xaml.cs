using BalancoPatrimonial.App.Services;
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

        // Aplica tema salvo (fire-and-forget — não bloqueia startup)
        _ = AplicarTemaSalvoAsync();

        // Aquecimento da IA para reduzir latência do 1º uso
        _ = WarmUpIaAsync();

        // Inicia na LoginPage. Após autenticar, a própria LoginPage troca pra AppShell.
        var loginPage = services.GetRequiredService<LoginPage>();
        MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#1F3A60"),
            BarTextColor = Colors.White
        };
    }

    /// <summary>
    /// Lê o tema persistido no SQLite (via ConfiguracaoLocalService) e aplica
    /// no app. Roda em background pra não atrasar a primeira renderização.
    /// </summary>
    private async Task AplicarTemaSalvoAsync()
    {
        try
        {
            var config = Services.GetService<IConfiguracaoLocalService>();
            if (config is null) return;

            var tema = await config.ObterTemaAsync();
            UserAppTheme = tema == "escuro" ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
            // Falha silenciosa — tema é cosmético, não pode quebrar o app
        }
    }

    private async Task WarmUpIaAsync()
    {
        try
        {
            var iaService = Services.GetService<IPdfAiAnalyzerService>();
            if (iaService != null && await iaService.ConfiguradoAsync())
            {
                await iaService.TestarConexaoAsync();
            }
        }
        catch { }
    }
}
