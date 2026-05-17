using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace BalancoPatrimonial.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegistrarDependencias(builder.Services);

        return builder.Build();
    }

    /// <summary>
    /// Registra todas as dependências da aplicação (Services, Controllers, Views).
    /// As DAOs serão adicionadas na Fase 2 quando entrarem as conexões com os bancos.
    /// </summary>
    private static void RegistrarDependencias(IServiceCollection services)
    {
        // Sessão do usuário — singleton (estado global de autenticação)
        services.AddSingleton<ISessaoUsuario, SessaoUsuario>();

        // Services — singletons por padrão (stateless)
        services.AddSingleton<IAuthService, AuthService>();

        // Controllers — transient (uma instância por uso)
        services.AddTransient<ILoginController, LoginController>();

        // Views — transient (cada navegação cria nova página)
        services.AddTransient<LoginPage>();
        services.AddTransient<PlanilhamentoPage>();
        services.AddTransient<EmpresasPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<AreaTestesPage>();
    }
}
