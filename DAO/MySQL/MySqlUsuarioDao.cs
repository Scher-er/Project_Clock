using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using Dapper;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// Implementação MySQL de <see cref="IUsuarioDao"/>.
/// Refatorado para utilizar o micro-ORM Dapper.
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
            VALUES (@Nome, @Login, @Email, @SenhaHash, @Perfil, @Ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        u.Id = await conn.ExecuteScalarAsync<int>(sql, u);
        return u.Id;
    }

    public async Task<bool> AtualizarAsync(Usuario u)
    {
        const string sql = @"
            UPDATE usuario
               SET nome=@Nome, email=@Email, perfil=@Perfil, ativo=@Ativo
             WHERE id=@Id;";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, u) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        const string sql = "DELETE FROM usuario WHERE id=@Id;";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<Usuario?> BuscarPorIdAsync(int id)
    {
        const string sql = @"
            SELECT id, nome, login, email, senha_hash AS SenhaHash, perfil, ativo, 
                   data_cadastro AS DataCriacao, ultimo_login AS UltimoLogin 
            FROM usuario WHERE id=@Id LIMIT 1;";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { Id = id });
    }

    public async Task<Usuario?> BuscarPorLoginAsync(string login)
    {
        const string sql = @"
            SELECT id, nome, login, email, senha_hash AS SenhaHash, perfil, ativo, 
                   data_cadastro AS DataCriacao, ultimo_login AS UltimoLogin 
            FROM usuario WHERE login=@Login LIMIT 1;";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Usuario>(sql, new { Login = login });
    }

    public async Task<IEnumerable<Usuario>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT id, nome, login, email, senha_hash AS SenhaHash, perfil, ativo, 
                   data_cadastro AS DataCriacao, ultimo_login AS UltimoLogin 
            FROM usuario ORDER BY nome;";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<Usuario>(sql);
    }

    public async Task<bool> AtualizarUltimoLoginAsync(int usuarioId)
    {
        const string sql = "UPDATE usuario SET ultimo_login=NOW() WHERE id=@Id;";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, new { Id = usuarioId }) > 0;
    }
}
