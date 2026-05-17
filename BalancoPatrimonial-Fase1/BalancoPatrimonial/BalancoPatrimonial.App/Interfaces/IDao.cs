namespace BalancoPatrimonial.App.Interfaces;

/// <summary>
/// Contrato base para toda classe DAO (Data Access Object).
/// Define o conjunto mínimo de operações de persistência que toda DAO deve oferecer.
///
/// Implementações específicas (MySqlEmpresaDAO, SqliteConfiguracaoDAO, MongoLogDAO...)
/// estendem esta interface e adicionam métodos próprios quando necessário.
/// </summary>
/// <typeparam name="T">Tipo da entidade persistida (Empresa, Balanco, Log, etc).</typeparam>
public interface IDao<T> where T : class
{
    /// <summary>Nome da fonte de dados (ex: "MySQL.Empresa", "SQLite.Configuracao").</summary>
    string Fonte { get; }

    /// <summary>Insere uma nova entidade e retorna o ID gerado.</summary>
    Task<int> InserirAsync(T entidade);

    /// <summary>Atualiza uma entidade existente. Retorna true se algo foi alterado.</summary>
    Task<bool> AtualizarAsync(T entidade);

    /// <summary>Exclui uma entidade pelo ID. Retorna true se algo foi removido.</summary>
    Task<bool> ExcluirAsync(int id);

    /// <summary>Busca uma entidade pelo ID. Retorna null se não encontrada.</summary>
    Task<T?> BuscarPorIdAsync(int id);

    /// <summary>Lista todas as entidades. Use com filtros nas DAOs específicas pra paginação.</summary>
    Task<IEnumerable<T>> ListarTodosAsync();
}
