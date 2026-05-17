using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Serviço de autenticação. Encapsula a lógica de login/logout e o
/// hashing/verificação de senha (será implementado na Fase 3).
/// </summary>
public interface IAuthService : IService
{
    Task<ResultadoOperacao<Usuario>> AutenticarAsync(string login, string senha);
    void EncerrarSessao();
}
