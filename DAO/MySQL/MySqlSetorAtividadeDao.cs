using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlSetorAtividadeDao : ISetorAtividadeDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.setor_atividade";

    public MySqlSetorAtividadeDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(SetorAtividade s)
    {
        const string sql = @"
            INSERT INTO setor_atividade (codigo, nome, descricao)
            VALUES (@codigo, @nome, @descricao);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@codigo", s.Codigo);
        cmd.Parameters.AddWithValue("@nome", s.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)s.Descricao ?? DBNull.Value);

        s.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return s.Id;
    }

    public async Task<bool> AtualizarAsync(SetorAtividade s)
    {
        const string sql = @"UPDATE setor_atividade
                             SET codigo=@codigo, nome=@nome, descricao=@descricao
                             WHERE id=@id";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", s.Id);
        cmd.Parameters.AddWithValue("@codigo", s.Codigo);
        cmd.Parameters.AddWithValue("@nome", s.Nome);
        cmd.Parameters.AddWithValue("@descricao", (object?)s.Descricao ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("DELETE FROM setor_atividade WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<SetorAtividade?> BuscarPorIdAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM setor_atividade WHERE id=@id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<SetorAtividade?> BuscarPorCodigoAsync(string codigo)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM setor_atividade WHERE codigo=@codigo LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@codigo", codigo);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<IEnumerable<SetorAtividade>> ListarTodosAsync()
    {
        var lista = new List<SetorAtividade>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM setor_atividade ORDER BY codigo", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    public async Task<IEnumerable<SetorAtividade>> ListarPorEmpresaAsync(int empresaId)
    {
        const string sql = @"
            SELECT s.*
              FROM setor_atividade s
              JOIN empresa_setor es ON es.setor_id = s.id
             WHERE es.empresa_id = @empresaId
             ORDER BY es.principal DESC, s.codigo";

        var lista = new List<SetorAtividade>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@empresaId", empresaId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    private static SetorAtividade Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        Codigo = r.GetString("codigo"),
        Nome = r.GetString("nome"),
        Descricao = r.IsDBNull(r.GetOrdinal("descricao")) ? null : r.GetString("descricao")
    };
}
