using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using Dapper;

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
            await conn.ExecuteAsync(
                "UPDATE dre SET ativado=0 WHERE empresa_id=@EmpresaId AND ano_exercicio=@AnoExercicio AND tipo_balanco=@TipoBalanco AND ativado=1",
                new { EmpresaId = d.EmpresaId, AnoExercicio = d.AnoExercicio, TipoBalanco = (int)d.TipoBalanco },
                tx);

            const string sql = @"
                INSERT INTO dre (empresa_id, ano_exercicio, tipo_balanco,
                    receita_liquida, lucro_bruto, resultado_operacional,
                    despesas_financeiras, lucro_liquido,
                    usuario_id, origem, hash_origem_pdf)
                VALUES (@EmpresaId, @AnoExercicio, @TipoBalanco, 
                        @ReceitaLiquida, @LucroBruto, @ResultadoOperacional,
                        @DespesasFinanceiras, @LucroLiquido, 
                        @UsuarioId, @Origem, @HashOrigemPdf);
                SELECT LAST_INSERT_ID();";

            var id = await conn.ExecuteScalarAsync<int>(sql, d, tx);

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
        const string sql = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   tipo_balanco AS TipoBalanco, receita_liquida AS ReceitaLiquida, 
                   lucro_bruto AS LucroBruto, resultado_operacional AS ResultadoOperacional, 
                   despesas_financeiras AS DespesasFinanceiras, lucro_liquido AS LucroLiquido, 
                   usuario_id AS UsuarioId, origem AS Origem, data_planilhamento AS DataPlanilhamento
            FROM dre
            WHERE empresa_id=@EmpresaId AND ano_exercicio=@AnoExercicio AND tipo_balanco=@TipoBalanco AND ativado=1
            LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Dre>(sql, new { EmpresaId = empresaId, AnoExercicio = anoExercicio, TipoBalanco = (int)tipo });
    }

    public async Task<IEnumerable<Dre>> ListarPorEmpresaAsync(int empresaId)
    {
        const string sql = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   tipo_balanco AS TipoBalanco, receita_liquida AS ReceitaLiquida, 
                   lucro_bruto AS LucroBruto, resultado_operacional AS ResultadoOperacional, 
                   despesas_financeiras AS DespesasFinanceiras, lucro_liquido AS LucroLiquido, 
                   usuario_id AS UsuarioId, origem AS Origem, data_planilhamento AS DataPlanilhamento
            FROM dre WHERE empresa_id=@EmpresaId AND ativado=1 ORDER BY ano_exercicio";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<Dre>(sql, new { EmpresaId = empresaId });
    }
}
