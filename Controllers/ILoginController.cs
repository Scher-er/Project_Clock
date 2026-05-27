using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Controllers;

/// <summary>
/// LoginController não tem CRUD, por isso herda apenas de IController (e não de IController&lt;T&gt;).
/// </summary>
public interface ILoginController : IController
{
    Task<ResultadoOperacao<Usuario>> AutenticarAsync(string login, string senha);
    void Sair();
}
