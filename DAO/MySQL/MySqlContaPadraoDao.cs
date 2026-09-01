using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using Dapper;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlContaPadraoDao : IContaPadraoDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.conta_padrao";

    public MySqlContaPadraoDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(ContaPadrao c)
    {
        const string sql = @"
            INSERT INTO conta_padrao
                (codigo, descricao, grupo_principal, conta_pai_id, nivel, eh_totalizadora, ordem, ativa)
            VALUES
                (@Codigo, @Descricao, @GrupoPrincipal, @ContaPaiId, @Nivel, @EhTotalizadora, @Ordem, @Ativa);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        c.Id = await conn.ExecuteScalarAsync<int>(sql, c);
        return c.Id;
    }

    public async Task<bool> AtualizarAsync(ContaPadrao c)
    {
        const string sql = @"
            UPDATE conta_padrao
               SET descricao=@Descricao, ordem=@Ordem, ativa=@Ativa
             WHERE id=@Id";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, c) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync("DELETE FROM conta_padrao WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<ContaPadrao?> BuscarPorIdAsync(int id)
    {
        const string sql = @"
            SELECT id AS Id, codigo AS Codigo, descricao AS Descricao, grupo_principal AS GrupoPrincipal,
                   conta_pai_id AS ContaPaiId, nivel AS Nivel, eh_totalizadora AS EhTotalizadora,
                   ordem AS Ordem, ativa AS Ativa
            FROM conta_padrao WHERE id=@Id LIMIT 1";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<ContaPadrao>(sql, new { Id = id });
    }

    public async Task<ContaPadrao?> BuscarPorCodigoAsync(string codigo)
    {
        const string sql = @"
            SELECT id AS Id, codigo AS Codigo, descricao AS Descricao, grupo_principal AS GrupoPrincipal,
                   conta_pai_id AS ContaPaiId, nivel AS Nivel, eh_totalizadora AS EhTotalizadora,
                   ordem AS Ordem, ativa AS Ativa
            FROM conta_padrao WHERE codigo=@Codigo LIMIT 1";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<ContaPadrao>(sql, new { Codigo = codigo });
    }

    public async Task<IEnumerable<ContaPadrao>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT id AS Id, codigo AS Codigo, descricao AS Descricao, grupo_principal AS GrupoPrincipal,
                   conta_pai_id AS ContaPaiId, nivel AS Nivel, eh_totalizadora AS EhTotalizadora,
                   ordem AS Ordem, ativa AS Ativa
            FROM conta_padrao WHERE ativa=1 ORDER BY ordem, codigo";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<ContaPadrao>(sql);
    }

    public async Task<IEnumerable<ContaPadrao>> ListarPorGrupoAsync(GrupoContaPrincipal grupo)
    {
        const string sql = @"
            SELECT id AS Id, codigo AS Codigo, descricao AS Descricao, grupo_principal AS GrupoPrincipal,
                   conta_pai_id AS ContaPaiId, nivel AS Nivel, eh_totalizadora AS EhTotalizadora,
                   ordem AS Ordem, ativa AS Ativa
            FROM conta_padrao WHERE grupo_principal=@Grupo AND ativa=1 ORDER BY ordem, codigo";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<ContaPadrao>(sql, new { Grupo = (byte)grupo });
    }

    public async Task<IEnumerable<ContaPadrao>> ListarHierarquiaAsync()
    {
        var todas = (await ListarTodosAsync()).ToList();
        var porId = todas.ToDictionary(c => c.Id);
        var raizes = new List<ContaPadrao>();

        foreach (var c in todas)
        {
            if (c.ContaPaiId is null)
            {
                raizes.Add(c);
            }
            else if (porId.TryGetValue(c.ContaPaiId.Value, out var pai))
            {
                pai.Filhas.Add(c);
                c.ContaPai = pai;
            }
        }
        return raizes;
    }
}
