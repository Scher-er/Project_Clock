using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Services;

public class SessaoUsuario : ISessaoUsuario
{
    public string NomeServico => "SessaoUsuario";

    public Usuario? UsuarioAtual { get; private set; }
    public bool Autenticado => UsuarioAtual is not null;

    public void IniciarSessao(Usuario usuario)
    {
        UsuarioAtual = usuario;
        usuario.UltimoLogin = DateTime.Now;
    }

    public void EncerrarSessao()
    {
        UsuarioAtual = null;
    }
}
