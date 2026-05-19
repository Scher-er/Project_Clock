using BalancoPatrimonial.App.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.Interfaces;

/// <summary>
/// Acesso a dados de Usuario. Adiciona ao CRUD genérico métodos específicos
/// que a autenticação precisa.
/// </summary>
public interface IUsuarioDao : IDao<Usuario>
{
    Task<Usuario?> BuscarPorLoginAsync(string login);
    Task<bool> AtualizarUltimoLoginAsync(int usuarioId);
}
