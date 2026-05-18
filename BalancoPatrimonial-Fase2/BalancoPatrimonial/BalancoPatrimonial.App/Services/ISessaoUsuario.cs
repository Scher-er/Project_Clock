using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

/// <summary>
/// Mantém o estado da sessão atual (usuário logado).
/// Registrado como singleton no DI — vive enquanto o app estiver aberto.
/// </summary>
public interface ISessaoUsuario : IService
{
    Usuario? UsuarioAtual { get; }
    bool Autenticado { get; }
    void IniciarSessao(Usuario usuario);
    void EncerrarSessao();
}
