using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// STUB de Fase 1 — autenticação local com usuário fixo (admin/admin).
///
/// Na Fase 3 vamos:
///   1. Receber um IUsuarioDao via construtor
///   2. Buscar o usuário pelo login no MySQL
///   3. Verificar a senha contra <see cref="Usuario.SenhaHash"/> usando BCrypt
///   4. Registrar o login no MongoDB (LogService)
/// </summary>
public class AuthService : IAuthService
{
    private readonly ISessaoUsuario _sessao;
    public string NomeServico => "AuthService";

    public AuthService(ISessaoUsuario sessao)
    {
        _sessao = sessao;
    }

    public async Task<ResultadoOperacao<Usuario>> AutenticarAsync(string login, string senha)
    {
        // Validações básicas
        if (string.IsNullOrWhiteSpace(login))
            return ResultadoOperacao<Usuario>.Falha("Informe o usuário.");
        if (string.IsNullOrWhiteSpace(senha))
            return ResultadoOperacao<Usuario>.Falha("Informe a senha.");

        // Simula latência de I/O (será real quando bater no MySQL)
        await Task.Delay(250);

        // === STUB FASE 1 — REMOVER NA FASE 3 ===
        if (login.Equals("admin", StringComparison.OrdinalIgnoreCase) && senha == "admin")
        {
            var usuario = new Usuario
            {
                Id = 1,
                Nome = "Administrador",
                Login = "admin",
                Email = "admin@banco.local",
                Perfil = "Administrador",
                Ativo = true,
                DataCriacao = DateTime.Now.AddYears(-1)
            };
            _sessao.IniciarSessao(usuario);
            return ResultadoOperacao<Usuario>.Ok(usuario, "Autenticado com sucesso.");
        }
        // ========================================

        return ResultadoOperacao<Usuario>.Falha("Usuário ou senha inválidos.");
    }

    public void EncerrarSessao()
    {
        _sessao.EncerrarSessao();
    }
}
