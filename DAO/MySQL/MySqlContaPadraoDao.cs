using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// DAO do plano de contas padrão. Lê majoritariamente — as contas são
/// criadas pelo seed e raramente alteradas em produção.
/// </summary>
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
                (@codigo, @descricao, @grupo, @pai, @nivel, @tot, @ordem, @ativa);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@codigo", c.Codigo);
        cmd.Parameters.AddWithValue("@descricao", c.Descricao);
        cmd.Parameters.AddWithValue("@grupo", (byte)c.GrupoPrincipal);
        cmd.Parameters.AddWithValue("@pai", (object?)c.ContaPaiId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@nivel", c.Nivel);
        cmd.Parameters.AddWithValue("@tot", c.EhTotalizadora);
        cmd.Parameters.AddWithValue("@ordem", c.Ordem);
        cmd.Parameters.AddWithValue("@ativa", c.Ativa);

        c.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return c.Id;
    }

    public async Task<bool> AtualizarAsync(ContaPadrao c)
    {
        const string sql = @"
            UPDATE conta_padrao
               SET descricao=@descricao, ordem=@ordem, ativa=@ativa
             WHERE id=@id";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", c.Id);
        cmd.Parameters.AddWithValue("@descricao", c.Descricao);
        cmd.Parameters.AddWithValue("@ordem", c.Ordem);
        cmd.Parameters.AddWithValue("@ativa", c.Ativa);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("DELETE FROM conta_padrao WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<ContaPadrao?> BuscarPorIdAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM conta_padrao WHERE id=@id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<ContaPadrao?> BuscarPorCodigoAsync(string codigo)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM conta_padrao WHERE codigo=@c LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@c", codigo);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<IEnumerable<ContaPadrao>> ListarTodosAsync()
    {
        var lista = new List<ContaPadrao>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "SELECT * FROM conta_padrao WHERE ativa=1 ORDER BY ordem, codigo", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    public async Task<IEnumerable<ContaPadrao>> ListarPorGrupoAsync(GrupoContaPrincipal grupo)
    {
        var lista = new List<ContaPadrao>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "SELECT * FROM conta_padrao WHERE grupo_principal=@g AND ativa=1 ORDER BY ordem, codigo", conn);
        cmd.Parameters.AddWithValue("@g", (byte)grupo);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    public async Task<IEnumerable<ContaPadrao>> ListarHierarquiaAsync()
    {
        // Carrega tudo em memória e monta a árvore
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

    private static ContaPadrao Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        Codigo = r.GetString("codigo"),
        Descricao = r.GetString("descricao"),
        GrupoPrincipal = (GrupoContaPrincipal)r.GetByte("grupo_principal"),
        ContaPaiId = r.IsDBNull(r.GetOrdinal("conta_pai_id")) ? null : r.GetInt32("conta_pai_id"),
        Nivel = r.GetByte("nivel"),
        EhTotalizadora = r.GetBoolean("eh_totalizadora"),
        Ordem = r.GetInt32("ordem"),
        Ativa = r.GetBoolean("ativa")
    };
}
