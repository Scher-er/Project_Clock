using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// Cria conexões com o MySQL. Padrão Factory — DAOs pedem conexões a esta classe
/// em vez de instanciar diretamente. Facilita troca de driver ou pooling no futuro.
/// </summary>
public interface IMySqlConnectionFactory
{
    /// <summary>Cria e ABRE uma nova conexão. O chamador é responsável por fechar (use 'await using').</summary>
    Task<MySqlConnection> AbrirConexaoAsync(CancellationToken ct = default);

    /// <summary>Testa se o MySQL está acessível com as credenciais configuradas.</summary>
    Task<bool> TestarConexaoAsync(CancellationToken ct = default);

    string ConnectionStringSegura { get; }
}

public class MySqlConnectionFactory : IMySqlConnectionFactory
{
    private readonly DatabaseSettings _settings;

    public MySqlConnectionFactory(DatabaseSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Connection string sem senha — pra usar em mensagens de erro/log.</summary>
    public string ConnectionStringSegura =>
        $"Server={_settings.MySqlServer};Database={_settings.MySqlDatabase};User={_settings.MySqlUser}";

    public async Task<MySqlConnection> AbrirConexaoAsync(CancellationToken ct = default)
    {
        var conn = new MySqlConnection(_settings.MySqlConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    public async Task<bool> TestarConexaoAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await AbrirConexaoAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
