using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.MongoDB;
using BalancoPatrimonial.App.DAO.MySQL;
using BalancoPatrimonial.App.DAO.SQLite;
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

    private static void RegistrarDependencias(IServiceCollection services)
    {
        // ───── Configuração (singleton — uma instância pra toda a app) ─────
        services.AddSingleton(DatabaseSettings.CarregarPadroes());

        // ───── Connection Factories (singleton — pooling interno) ─────
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IMongoConnectionFactory, MongoConnectionFactory>();

        // ───── DAOs (Fase 3) ─────
        // services.AddTransient<IEmpresaDao, MySqlEmpresaDao>();
        // services.AddTransient<IBalancoDao, MySqlBalancoDao>();
        // services.AddTransient<IConfiguracaoLocalDao, SqliteConfiguracaoLocalDao>();
        // services.AddTransient<ILogDao, MongoLogDao>();

        // ───── Sessão e Services ─────
        services.AddSingleton<ISessaoUsuario, SessaoUsuario>();
        services.AddSingleton<IAuthService, AuthService>();

        // ───── Controllers ─────
        services.AddTransient<ILoginController, LoginController>();

        // ───── Views ─────
        services.AddTransient<LoginPage>();
        services.AddTransient<PlanilhamentoPage>();
        services.AddTransient<EmpresasPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<AreaTestesPage>();
    }
}
