using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// Implementação MySQL de <see cref="IUsuarioDao"/>.
///
/// Padrão das DAOs deste projeto:
///   - Recebe <see cref="IMySqlConnectionFactory"/> via construtor (DI)
///   - Cada método abre uma nova conexão com 'await using' (garante dispose)
///   - Usa parâmetros nomeados (@id) — NUNCA concatenar string SQL com input do usuário
///   - Retorna POCOs limpos da pasta Models, sem detalhe de banco vazando pra cima
/// </summary>
public class MySqlUsuarioDao : IUsuarioDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.usuario";

    public MySqlUsuarioDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(Usuario u)
    {
        const string sql = @"
            INSERT INTO usuario (nome, login, email, senha_hash, perfil, ativo)
            VALUES (@nome, @login, @email, @senha_hash, @perfil, @ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nome", u.Nome);
        cmd.Parameters.AddWithValue("@login", u.Login);
        cmd.Parameters.AddWithValue("@email", u.Email);
        cmd.Parameters.AddWithValue("@senha_hash", u.SenhaHash);
        cmd.Parameters.AddWithValue("@perfil", u.Perfil);
        cmd.Parameters.AddWithValue("@ativo", u.Ativo);

        var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        u.Id = id;
        return id;
    }

    public async Task<bool> AtualizarAsync(Usuario u)
    {
        const string sql = @"
            UPDATE usuario
               SET nome=@nome, email=@email, perfil=@perfil, ativo=@ativo
             WHERE id=@id;";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", u.Id);
        cmd.Parameters.AddWithValue("@nome", u.Nome);
        cmd.Parameters.AddWithValue("@email", u.Email);
        cmd.Parameters.AddWithValue("@perfil", u.Perfil);
        cmd.Parameters.AddWithValue("@ativo", u.Ativo);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("DELETE FROM usuario WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<Usuario?> BuscarPorIdAsync(int id)
    {
        const string sql = "SELECT * FROM usuario WHERE id=@id LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Mapear(reader) : null;
    }

    public async Task<Usuario?> BuscarPorLoginAsync(string login)
    {
        const string sql = "SELECT * FROM usuario WHERE login=@login LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@login", login);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Mapear(reader) : null;
    }

    public async Task<IEnumerable<Usuario>> ListarTodosAsync()
    {
        const string sql = "SELECT * FROM usuario ORDER BY nome";
        var lista = new List<Usuario>();

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(Mapear(reader));
        }
        return lista;
    }

    public async Task<bool> AtualizarUltimoLoginAsync(int usuarioId)
    {
        const string sql = "UPDATE usuario SET ultimo_login=NOW() WHERE id=@id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", usuarioId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>Centraliza o mapeamento DataReader → POCO. Evita duplicação.</summary>
    private static Usuario Mapear(MySqlDataReader r) => new()
    {
        Id           = r.GetInt32("id"),
        Nome         = r.GetString("nome"),
        Login        = r.GetString("login"),
        Email        = r.GetString("email"),
        SenhaHash    = r.GetString("senha_hash"),
        Perfil       = r.GetString("perfil"),
        Ativo        = r.GetBoolean("ativo"),
        DataCriacao  = r.GetDateTime("data_cadastro"),
        UltimoLogin  = r.IsDBNull(r.GetOrdinal("ultimo_login")) ? null : r.GetDateTime("ultimo_login")
    };
}
