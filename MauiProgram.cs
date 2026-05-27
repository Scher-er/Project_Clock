using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.Interfaces;
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

        // ───── DAOs (transient — leves, criados por uso) ─────
        // MySQL
        services.AddTransient<IUsuarioDao, MySqlUsuarioDao>();
        services.AddTransient<IGrupoEconomicoDao, MySqlGrupoEconomicoDao>();
        services.AddTransient<ISetorAtividadeDao, MySqlSetorAtividadeDao>();
        services.AddTransient<IEmpresaDao, MySqlEmpresaDao>();
        services.AddTransient<IContaPadraoDao, MySqlContaPadraoDao>();
        services.AddTransient<IBalancoDao, MySqlBalancoDao>();

        // SQLite
        services.AddTransient<IConfiguracaoLocalDao, SqliteConfiguracaoLocalDao>();

        // MongoDB
        services.AddSingleton<ILogDao, MongoLogDao>();   // singleton porque mantém collection cacheada

        // ───── Sessão e Services ─────
        services.AddSingleton<ISessaoUsuario, SessaoUsuario>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IConfiguracaoLocalService, ConfiguracaoLocalService>();
        services.AddTransient<IEmpresaService, EmpresaService>();
        services.AddTransient<IBalancoService, BalancoService>();
        services.AddTransient<IListagensService, ListagensService>();

        // ───── Controllers ─────
        services.AddTransient<ILoginController, LoginController>();
        services.AddTransient<IEmpresaController, EmpresaController>();
        services.AddTransient<IBalancoController, BalancoController>();
        services.AddTransient<ILogController, LogController>();

        // ───── Views ─────
        services.AddTransient<LoginPage>();
        services.AddTransient<PlanilhamentoPage>();
        services.AddTransient<EmpresasPage>();
        services.AddTransient<CadastroEmpresaPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<AreaTestesPage>();
    }
}
