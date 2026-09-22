using BalancoPatrimonial.App.Controllers;
using BalancoPatrimonial.App.DAO;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.DAO.Mongo;
using BalancoPatrimonial.App.DAO.MySQL;
using BalancoPatrimonial.App.DAO.SQLite;
using BalancoPatrimonial.App.Services;
using BalancoPatrimonial.App.Views;
using BalancoPatrimonial.App.ViewModels;
using CommunityToolkit.Maui;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace BalancoPatrimonial.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Declara uso da Community License do QuestPDF (obrigatório por contrato da lib).
        // O projeto é academico/sem fins lucrativos — qualifica pra license community.
        // Ver: https://www.questpdf.com/license/
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        // Configura paleta de cores e fonte padrão do LiveCharts2.
        // Em rc5+, isso é feito separado do builder via LiveCharts.Configure().
        LiveCharts.Configure(config => config
            .HasGlobalSKTypeface(SKTypeface.FromFamilyName("Segoe UI"))
            .AddDarkTheme()
            .AddLightTheme());

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()        // SkiaSharp DEVE vir chained em UseMauiApp
            .UseLiveCharts();      // LiveCharts rc5+ exige .UseLiveCharts() chained (sem argumento)

        // Nota: removi .ConfigureFonts() porque o app usa a fonte default do
        // sistema (Segoe UI no Windows). Pra adicionar fontes customizadas,
        // coloque o .ttf em Resources/Fonts/ e re-adicione a chamada aqui.

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
        services.AddTransient<IDreDao, MySqlDreDao>();
        // MySqlMapeamentoDeParaDao recebe string no construtor (legado), registrado via factory
        services.AddTransient<IMapeamentoDeParaDao>(sp =>
            new MySqlMapeamentoDeParaDao(sp.GetRequiredService<DatabaseSettings>().MySqlConnectionString));

        // SQLite
        services.AddTransient<IConfiguracaoLocalDao, SqliteConfiguracaoLocalDao>();

        // MongoDB
        services.AddSingleton<ILogDao, MongoLogDao>();
        services.AddSingleton<IAnaliseIaDao, MongoAnaliseIaDao>();

        // ───── Sessão e Services ─────
        services.AddSingleton<ISessaoUsuario, SessaoUsuario>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IConfiguracaoLocalService, ConfiguracaoLocalService>();
        services.AddTransient<IEmpresaService, EmpresaService>();
        services.AddTransient<IBalancoService, BalancoService>();
        services.AddTransient<IListagensService, ListagensService>();
        services.AddTransient<IPdfParserService, PdfParserService>();
        services.AddSingleton<IAiSettingsService, AiSettingsService>();
        services.AddTransient<IPdfAiAnalyzerService, GeminiPdfAnalyzerService>();
        services.AddTransient<IExportacaoService, ExportacaoService>();
        services.AddTransient<IAnaliseService, AnaliseService>();
        services.AddTransient<ICvmMockService, CvmMockService>();

        // ───── Controllers ─────
        services.AddTransient<ILoginController, LoginController>();
        services.AddTransient<IEmpresaController, EmpresaController>();
        services.AddTransient<IBalancoController, BalancoController>();
        services.AddTransient<ILogController, LogController>();
        services.AddTransient<IPlanilhamentoController, PlanilhamentoController>();
        services.AddTransient<IExportacaoController, ExportacaoController>();
        services.AddTransient<IAnaliseController, AnaliseController>();

        // ───── ViewModels ─────
        services.AddTransient<BalancoPatrimonial.App.ViewModels.LoginViewModel>();
        services.AddTransient<BalancoPatrimonial.App.ViewModels.EmpresasViewModel>();

        // ───── Views ─────
        services.AddTransient<LoginPage>();
        services.AddTransient<PlanilhamentoViewModel>();
        services.AddTransient<PlanilhamentoPage>();
        services.AddTransient<EmpresasPage>();
        services.AddTransient<CadastroEmpresaViewModel>();
        services.AddTransient<CadastroEmpresaPage>();
        services.AddTransient<BalancosEmpresaViewModel>();
        services.AddTransient<BalancosEmpresaPage>();
        services.AddTransient<DetalheBalancoPage>();

        services.AddTransient<AnalisesViewModel>();
        services.AddTransient<AnalisesPage>();

        services.AddTransient<RevisaoImportacaoViewModel>();
        services.AddTransient<RevisaoImportacaoPage>();
        services.AddTransient<LogsPage>();
        services.AddTransient<AreaTestesPage>();
        services.AddTransient<ComparacaoPage>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardPage>();
        services.AddTransient<PlanoContasViewModel>();
        services.AddTransient<PlanoContasPage>();
    }
}
