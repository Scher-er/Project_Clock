namespace BalancoPatrimonial.App.Interfaces;

/// <summary>
/// Contrato base para toda classe DAO (Data Access Object).
/// Generalizado em <typeparamref name="TId"/> pra suportar MySQL/SQLite (int)
/// e MongoDB (string/ObjectId) com a mesma interface.
/// </summary>
public interface IDao<T, TId> where T : class
{
    /// <summary>Nome da fonte de dados (ex: "MySQL.empresa", "Mongo.logs").</summary>
    string Fonte { get; }

    Task<TId> InserirAsync(T entidade);
    Task<bool> AtualizarAsync(T entidade);
    Task<bool> ExcluirAsync(TId id);
    Task<T?> BuscarPorIdAsync(TId id);
    Task<IEnumerable<T>> ListarTodosAsync();
}

/// <summary>
/// Atalho para o caso mais comum (ID inteiro auto-incremento — MySQL e SQLite).
/// </summary>
public interface IDao<T> : IDao<T, int> where T : class { }
