using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Services;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// Controller da tela de Login. Faz a ponte entre a View (LoginPage)
/// e o Service de autenticação (AuthService).
///
/// A View NUNCA chama o AuthService diretamente — sempre passa pelo Controller.
/// Isso permite trocar a implementação do Service sem mexer na View.
/// </summary>
public class LoginController : ILoginController
{
    private readonly IAuthService _authService;
    public string NomeRecurso => "Login";

    public LoginController(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<ResultadoOperacao<Usuario>> AutenticarAsync(string login, string senha)
        => _authService.AutenticarAsync(login, senha);

    public void Sair() => _authService.EncerrarSessao();
}
