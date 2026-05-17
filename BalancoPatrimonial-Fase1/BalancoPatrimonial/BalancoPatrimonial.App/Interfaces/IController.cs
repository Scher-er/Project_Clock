using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.Interfaces;

/// <summary>
/// Marcador base — todo Controller implementa esta interface.
/// Permite tratar Controllers de forma uniforme (logging, descoberta via DI, etc).
/// </summary>
public interface IController
{
    /// <summary>Nome do recurso controlado (ex: "Empresa", "Balanco", "Login").</summary>
    string NomeRecurso { get; }
}

/// <summary>
/// Contrato genérico de Controller para entidades com CRUD.
/// LoginController NÃO usa esta versão (não tem CRUD), apenas IController.
/// </summary>
/// <typeparam name="T">Tipo da entidade gerenciada.</typeparam>
public interface IController<T> : IController where T : class
{
    Task<ResultadoOperacao<int>> CriarAsync(T entidade);
    Task<ResultadoOperacao<bool>> AtualizarAsync(T entidade);
    Task<ResultadoOperacao<bool>> ExcluirAsync(int id);
    Task<ResultadoOperacao<T?>> BuscarPorIdAsync(int id);
    Task<ResultadoOperacao<IEnumerable<T>>> ListarAsync();
}
