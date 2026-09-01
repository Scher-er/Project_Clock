using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using Dapper;

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
            VALUES (@Codigo, @Nome, @Descricao);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        s.Id = await conn.ExecuteScalarAsync<int>(sql, s);
        return s.Id;
    }

    public async Task<bool> AtualizarAsync(SetorAtividade s)
    {
        const string sql = @"UPDATE setor_atividade
                             SET codigo=@Codigo, nome=@Nome, descricao=@Descricao
                             WHERE id=@Id";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, s) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync("DELETE FROM setor_atividade WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<SetorAtividade?> BuscarPorIdAsync(int id)
    {
        const string sql = @"SELECT id AS Id, codigo AS Codigo, nome AS Nome, descricao AS Descricao FROM setor_atividade WHERE id=@Id LIMIT 1";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<SetorAtividade>(sql, new { Id = id });
    }

    public async Task<SetorAtividade?> BuscarPorCodigoAsync(string codigo)
    {
        const string sql = @"SELECT id AS Id, codigo AS Codigo, nome AS Nome, descricao AS Descricao FROM setor_atividade WHERE codigo=@Codigo LIMIT 1";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<SetorAtividade>(sql, new { Codigo = codigo });
    }

    public async Task<IEnumerable<SetorAtividade>> ListarTodosAsync()
    {
        const string sql = @"SELECT id AS Id, codigo AS Codigo, nome AS Nome, descricao AS Descricao FROM setor_atividade ORDER BY codigo";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<SetorAtividade>(sql);
    }

    public async Task<IEnumerable<SetorAtividade>> ListarPorEmpresaAsync(int empresaId)
    {
        const string sql = @"
            SELECT s.id AS Id, s.codigo AS Codigo, s.nome AS Nome, s.descricao AS Descricao
              FROM setor_atividade s
              JOIN empresa_setor es ON es.setor_id = s.id
             WHERE es.empresa_id = @EmpresaId
             ORDER BY es.principal DESC, s.codigo";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<SetorAtividade>(sql, new { EmpresaId = empresaId });
    }
}
