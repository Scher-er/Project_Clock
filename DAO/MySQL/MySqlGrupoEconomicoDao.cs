using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using Dapper;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlGrupoEconomicoDao : IGrupoEconomicoDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.grupo_economico";

    public MySqlGrupoEconomicoDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(GrupoEconomico g)
    {
        const string sql = @"
            INSERT INTO grupo_economico (nome, descricao, ativo)
            VALUES (@Nome, @Descricao, @Ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        g.Id = await conn.ExecuteScalarAsync<int>(sql, g);
        return g.Id;
    }

    public async Task<bool> AtualizarAsync(GrupoEconomico g)
    {
        const string sql = @"
            UPDATE grupo_economico
               SET nome=@Nome, descricao=@Descricao, ativo=@Ativo
             WHERE id=@Id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, g) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync("DELETE FROM grupo_economico WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<GrupoEconomico?> BuscarPorIdAsync(int id)
    {
        const string sql = @"
            SELECT id AS Id, nome AS Nome, descricao AS Descricao, 
                   data_cadastro AS DataCadastro, ativo AS Ativo 
            FROM grupo_economico WHERE id=@Id LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<GrupoEconomico>(sql, new { Id = id });
    }

    public async Task<GrupoEconomico?> BuscarPorNomeAsync(string nome)
    {
        const string sql = @"
            SELECT id AS Id, nome AS Nome, descricao AS Descricao, 
                   data_cadastro AS DataCadastro, ativo AS Ativo 
            FROM grupo_economico WHERE nome=@Nome LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<GrupoEconomico>(sql, new { Nome = nome });
    }

    public async Task<IEnumerable<GrupoEconomico>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT id AS Id, nome AS Nome, descricao AS Descricao, 
                   data_cadastro AS DataCadastro, ativo AS Ativo 
            FROM grupo_economico ORDER BY nome";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<GrupoEconomico>(sql);
    }
}
