using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlDreDao : IDreDao
{
    private readonly IMySqlConnectionFactory _connFactory;

    public MySqlDreDao(IMySqlConnectionFactory connFactory) => _connFactory = connFactory;

    public async Task<int> SalvarComSubstituicaoAsync(Dre d)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // Soft-delete da DRE ativa anterior (mesma chave)
            await using (var del = new MySqlCommand(
                "UPDATE dre SET ativado=0 WHERE empresa_id=@e AND ano_exercicio=@a AND tipo_balanco=@t AND ativado=1",
                conn, tx))
            {
                del.Parameters.AddWithValue("@e", d.EmpresaId);
                del.Parameters.AddWithValue("@a", d.AnoExercicio);
                del.Parameters.AddWithValue("@t", (int)d.TipoBalanco);
                await del.ExecuteNonQueryAsync();
            }

            const string sql = @"
                INSERT INTO dre (empresa_id, ano_exercicio, tipo_balanco,
                    receita_liquida, lucro_bruto, resultado_operacional,
                    despesas_financeiras, lucro_liquido,
                    usuario_id, origem, hash_origem_pdf)
                VALUES (@e,@a,@t,@rl,@lb,@ro,@df,@ll,@u,@o,@h);
                SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@e", d.EmpresaId);
            cmd.Parameters.AddWithValue("@a", d.AnoExercicio);
            cmd.Parameters.AddWithValue("@t", (int)d.TipoBalanco);
            cmd.Parameters.AddWithValue("@rl", d.ReceitaLiquida);
            cmd.Parameters.AddWithValue("@lb", d.LucroBruto);
            cmd.Parameters.AddWithValue("@ro", d.ResultadoOperacional);
            cmd.Parameters.AddWithValue("@df", d.DespesasFinanceiras);
            cmd.Parameters.AddWithValue("@ll", d.LucroLiquido);
            cmd.Parameters.AddWithValue("@u", d.UsuarioId);
            cmd.Parameters.AddWithValue("@o", (object?)d.Origem ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@h", (object?)d.HashOrigemPdf ?? DBNull.Value);
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Dre?> BuscarAtivaAsync(int empresaId, int anoExercicio, TipoBalanco tipo)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            @"SELECT * FROM dre
              WHERE empresa_id=@e AND ano_exercicio=@a AND tipo_balanco=@t AND ativado=1
              LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        cmd.Parameters.AddWithValue("@a", anoExercicio);
        cmd.Parameters.AddWithValue("@t", (int)tipo);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<IEnumerable<Dre>> ListarPorEmpresaAsync(int empresaId)
    {
        var lista = new List<Dre>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "SELECT * FROM dre WHERE empresa_id=@e AND ativado=1 ORDER BY ano_exercicio", conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    private static Dre Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        EmpresaId = r.GetInt32("empresa_id"),
        AnoExercicio = r.GetInt32("ano_exercicio"),
        TipoBalanco = (TipoBalanco)r.GetInt32("tipo_balanco"),
        ReceitaLiquida = r.GetDecimal("receita_liquida"),
        LucroBruto = r.GetDecimal("lucro_bruto"),
        ResultadoOperacional = r.GetDecimal("resultado_operacional"),
        DespesasFinanceiras = r.GetDecimal("despesas_financeiras"),
        LucroLiquido = r.GetDecimal("lucro_liquido"),
        UsuarioId = r.GetInt32("usuario_id"),
        Origem = r.IsDBNull(r.GetOrdinal("origem")) ? null : r.GetString("origem"),
        DataPlanilhamento = r.GetDateTime("data_planilhamento")
    };
}
