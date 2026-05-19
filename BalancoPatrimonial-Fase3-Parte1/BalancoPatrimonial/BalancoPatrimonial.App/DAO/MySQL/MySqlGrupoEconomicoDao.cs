using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using MySqlConnector;

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
            VALUES (@nome, @descricao, @ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nome", g.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)g.Descricao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ativo", g.Ativo);

        g.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return g.Id;
    }

    public async Task<bool> AtualizarAsync(GrupoEconomico g)
    {
        const string sql = @"
            UPDATE grupo_economico
               SET nome=@nome, descricao=@descricao, ativo=@ativo
             WHERE id=@id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", g.Id);
        cmd.Parameters.AddWithValue("@nome", g.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)g.Descricao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ativo", g.Ativo);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("DELETE FROM grupo_economico WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<GrupoEconomico?> BuscarPorIdAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM grupo_economico WHERE id=@id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<GrupoEconomico?> BuscarPorNomeAsync(string nome)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM grupo_economico WHERE nome=@nome LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@nome", nome);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<IEnumerable<GrupoEconomico>> ListarTodosAsync()
    {
        var lista = new List<GrupoEconomico>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM grupo_economico ORDER BY nome", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    private static GrupoEconomico Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        Nome = r.GetString("nome"),
        Descricao = r.IsDBNull(r.GetOrdinal("descricao")) ? null : r.GetString("descricao"),
        DataCadastro = r.GetDateTime("data_cadastro"),
        Ativo = r.GetBoolean("ativo")
    };
}
