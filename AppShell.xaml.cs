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
            lblUsuarioLogado.Text = _sessao.UsuarioAtual.Nome;
        }

        // ─────────────────────────────────────────────────────────────────
        // FIX CRÍTICO: configura DataTemplates que usam DI pra resolver
        // as pages.
        //
        // Por que aqui e não no XAML?
        //   No XAML, "ContentTemplate=\"{DataTemplate views:PlanilhamentoPage}\""
        //   faz o MAUI chamar Activator.CreateInstance(typeof(PlanilhamentoPage))
        //   — ou seja, requer construtor sem parâmetros. Mas todas as nossas
        //   pages têm construtores com DI (IPlanilhamentoController, etc.),
        //   então a instanciação falha silenciosamente e a navegação não funciona.
        //
        //   Aqui usamos o overload DataTemplate(Func<object>) que aceita uma
        //   factory function — passamos uma lambda que resolve via App.Services,
        //   garantindo que o DI seja usado.
        // ─────────────────────────────────────────────────────────────────
        var sp = App.Services;
        scDashboard.ContentTemplate     = new DataTemplate(() => sp.GetRequiredService<DashboardPage>());
        scPlanilhamento.ContentTemplate = new DataTemplate(() => sp.GetRequiredService<PlanilhamentoPage>());
        scEmpresas.ContentTemplate      = new DataTemplate(() => sp.GetRequiredService<EmpresasPage>());
        scAnalises.ContentTemplate      = new DataTemplate(() => sp.GetRequiredService<AnalisesPage>());
        scComparacao.ContentTemplate    = new DataTemplate(() => sp.GetRequiredService<ComparacaoPage>());
        scLogs.ContentTemplate          = new DataTemplate(() => sp.GetRequiredService<LogsPage>());
        scTestes.ContentTemplate        = new DataTemplate(() => sp.GetRequiredService<AreaTestesPage>());

        Routing.RegisterRoute(nameof(CadastroEmpresaPage), typeof(CadastroEmpresaPage));
    }

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
