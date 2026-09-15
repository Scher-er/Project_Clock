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

        if (_sessao.Autenticado && _sessao.UsuarioAtual is not null)
        {
            lblUsuarioLogado.Text = $"👤 {_sessao.UsuarioAtual.Nome}";
        }

        // Configura DataTemplates com DI — factory lambdas protegidas
        var sp = App.Services;
        scDashboard.ContentTemplate     = CriarTemplate<DashboardPage>(sp);
        scEmpresas.ContentTemplate      = CriarTemplate<EmpresasPage>(sp);
        scPlanilhamento.ContentTemplate = CriarTemplate<PlanilhamentoPage>(sp);
        scAnalises.ContentTemplate      = CriarTemplate<AnalisesPage>(sp);
        scComparacao.ContentTemplate    = CriarTemplate<ComparacaoPage>(sp);
        scLogs.ContentTemplate          = CriarTemplate<LogsPage>(sp);
        scTestes.ContentTemplate        = CriarTemplate<AreaTestesPage>(sp);

        Routing.RegisterRoute(nameof(CadastroEmpresaPage), typeof(CadastroEmpresaPage));
    }

    /// <summary>
    /// Cria DataTemplate que resolve a página via DI.
    /// Se a resolução falhar, retorna uma ContentPage de erro
    /// em vez de lançar exceção (que travaria a Shell inteira).
    /// </summary>
    private static DataTemplate CriarTemplate<T>(IServiceProvider sp) where T : Page
    {
        return new DataTemplate(() =>
        {
            try
            {
                return sp.GetRequiredService<T>();
            }
            catch (Exception ex)
            {
                return new ContentPage
                {
                    Content = new VerticalStackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center,
                        Spacing = 12,
                        Children =
                        {
                            new Label
                            {
                                Text = $"Erro ao carregar {typeof(T).Name}",
                                FontSize = 18,
                                FontAttributes = FontAttributes.Bold,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            new Label
                            {
                                Text = ex.Message,
                                FontSize = 13,
                                TextColor = Colors.Gray,
                                HorizontalOptions = LayoutOptions.Center
                            }
                        }
                    }
                };
            }
        });
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
