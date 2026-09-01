using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using Dapper;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlBalancoDao : IBalancoDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.balanco";

    public MySqlBalancoDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(Balanco b)
    {
        const string sql = @"
            INSERT INTO balanco
                (empresa_id, ano_exercicio, data_referencia, tipo_balanco,
                 usuario_id, origem, hash_origem_pdf, observacoes,
                 moeda, multiplicador_valores)
            VALUES
                (@EmpresaId, @AnoExercicio, @DataReferencia, @TipoBalanco,
                 @UsuarioId, @Origem, @HashOrigemPdf, @Observacoes,
                 @Moeda, @MultiplicadorValores);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        b.Id = await conn.ExecuteScalarAsync<int>(sql, b);
        return b.Id;
    }

    public async Task<int> InserirComContasAsync(Balanco b)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string sqlBalanco = @"
                INSERT INTO balanco
                    (empresa_id, ano_exercicio, data_referencia, tipo_balanco,
                     usuario_id, origem, hash_origem_pdf, observacoes,
                     moeda, multiplicador_valores)
                VALUES
                    (@EmpresaId, @AnoExercicio, @DataReferencia, @TipoBalanco,
                     @UsuarioId, @Origem, @HashOrigemPdf, @Observacoes,
                     @Moeda, @MultiplicadorValores);
                SELECT LAST_INSERT_ID();";

            b.Id = await conn.ExecuteScalarAsync<int>(sqlBalanco, b, tx);

            const string sqlConta = @"
                INSERT INTO conta_balanco
                    (balanco_id, conta_padrao_id, valor, descricao_original)
                VALUES
                    (@BalancoId, @ContaPadraoId, @Valor, @DescricaoOriginal)";

            foreach (var cb in b.Contas)
            {
                cb.BalancoId = b.Id;
            }
            await conn.ExecuteAsync(sqlConta, b.Contas, tx);

            await tx.CommitAsync();
            return b.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AtualizarAsync(Balanco b)
    {
        const string sql = @"
            UPDATE balanco SET
                ano_exercicio=@AnoExercicio, data_referencia=@DataReferencia, tipo_balanco=@TipoBalanco,
                origem=@Origem, hash_origem_pdf=@HashOrigemPdf, observacoes=@Observacoes,
                moeda=@Moeda, multiplicador_valores=@MultiplicadorValores
            WHERE id=@Id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, b) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync("UPDATE balanco SET ativado=0 WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<int?> BuscarIdAtivoAsync(int empresaId, int anoExercicio, TipoBalanco tipo)
    {
        const string sql = @"SELECT id FROM balanco
                             WHERE empresa_id=@e AND ano_exercicio=@a AND tipo_balanco=@t AND ativado=1
                             LIMIT 1";
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteScalarAsync<int?>(sql, new { e = empresaId, a = anoExercicio, t = (int)tipo });
    }

    public async Task<bool> AtualizarComContasAsync(Balanco b)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string sqlHeader = @"
                UPDATE balanco SET
                    ano_exercicio=@AnoExercicio, data_referencia=@DataReferencia, tipo_balanco=@TipoBalanco,
                    origem=@Origem, hash_origem_pdf=@HashOrigemPdf, observacoes=@Observacoes,
                    moeda=@Moeda, multiplicador_valores=@MultiplicadorValores
                WHERE id=@Id";
            await conn.ExecuteAsync(sqlHeader, b, tx);

            await conn.ExecuteAsync("DELETE FROM conta_balanco WHERE balanco_id=@Id", new { Id = b.Id }, tx);

            const string sqlConta = @"
                INSERT INTO conta_balanco (balanco_id, conta_padrao_id, valor, descricao_original)
                VALUES (@BalancoId, @ContaPadraoId, @Valor, @DescricaoOriginal)";

            foreach (var cb in b.Contas)
            {
                cb.BalancoId = b.Id;
            }
            await conn.ExecuteAsync(sqlConta, b.Contas, tx);

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<Balanco?> BuscarPorIdAsync(int id)
    {
        const string sql = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   data_referencia AS DataReferencia, tipo_balanco AS TipoBalanco, 
                   data_planilhamento AS DataPlanilhamento, usuario_id AS UsuarioId, 
                   origem AS Origem, hash_origem_pdf AS HashOrigemPdf, observacoes AS Observacoes, 
                   moeda AS Moeda, multiplicador_valores AS MultiplicadorValores
            FROM balanco WHERE id=@Id AND ativado=1 LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Balanco>(sql, new { Id = id });
    }

    public async Task<Balanco?> BuscarCompletoAsync(int balancoId)
    {
        var b = await BuscarPorIdAsync(balancoId);
        if (b is null) return null;

        const string sql = @"
            SELECT cb.id AS Id, cb.balanco_id AS BalancoId, cb.conta_padrao_id AS ContaPadraoId, 
                   cb.valor AS Valor, cb.descricao_original AS DescricaoOriginal,
                   cp.id AS Id, cp.codigo AS Codigo, cp.descricao AS Descricao, 
                   cp.grupo_principal AS GrupoPrincipal, cp.conta_pai_id AS ContaPaiId, 
                   cp.nivel AS Nivel, cp.eh_totalizadora AS EhTotalizadora, cp.ordem AS Ordem, cp.ativa AS Ativa
              FROM conta_balanco cb
              JOIN conta_padrao cp ON cp.id = cb.conta_padrao_id
             WHERE cb.balanco_id = @Bal
             ORDER BY cp.ordem, cp.codigo";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        b.Contas = (await conn.QueryAsync<ContaBalanco, ContaPadrao, ContaBalanco>(
            sql,
            (cb, cp) =>
            {
                cb.ContaPadrao = cp;
                return cb;
            },
            new { Bal = balancoId },
            splitOn: "Id"
        )).ToList();

        return b;
    }

    public async Task<IEnumerable<Balanco>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   data_referencia AS DataReferencia, tipo_balanco AS TipoBalanco, 
                   data_planilhamento AS DataPlanilhamento, usuario_id AS UsuarioId, 
                   origem AS Origem, hash_origem_pdf AS HashOrigemPdf, observacoes AS Observacoes, 
                   moeda AS Moeda, multiplicador_valores AS MultiplicadorValores
            FROM balanco WHERE ativado=1 ORDER BY ano_exercicio DESC, empresa_id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<Balanco>(sql);
    }

    public async Task<IEnumerable<Balanco>> ListarPorEmpresaAsync(int empresaId)
    {
        const string sqlCabecalhos = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   data_referencia AS DataReferencia, tipo_balanco AS TipoBalanco, 
                   data_planilhamento AS DataPlanilhamento, usuario_id AS UsuarioId, 
                   origem AS Origem, hash_origem_pdf AS HashOrigemPdf, observacoes AS Observacoes, 
                   moeda AS Moeda, multiplicador_valores AS MultiplicadorValores
            FROM balanco WHERE empresa_id=@EmpresaId AND ativado=1 ORDER BY ano_exercicio DESC";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        var lista = (await conn.QueryAsync<Balanco>(sqlCabecalhos, new { EmpresaId = empresaId })).ToList();

        if (lista.Count == 0) return lista;

        var porId = lista.ToDictionary(b => b.Id, b => b);
        const string sqlContas = @"
            SELECT cb.id AS Id, cb.balanco_id AS BalancoId, cb.conta_padrao_id AS ContaPadraoId, 
                   cb.valor AS Valor, cb.descricao_original AS DescricaoOriginal,
                   cp.id AS Id, cp.codigo AS Codigo, cp.descricao AS Descricao, 
                   cp.grupo_principal AS GrupoPrincipal, cp.conta_pai_id AS ContaPaiId, 
                   cp.nivel AS Nivel, cp.eh_totalizadora AS EhTotalizadora, cp.ordem AS Ordem, cp.ativa AS Ativa
              FROM conta_balanco cb
              JOIN conta_padrao cp ON cp.id = cb.conta_padrao_id
              JOIN balanco b      ON b.id  = cb.balanco_id
             WHERE b.empresa_id = @EmpresaId
             ORDER BY cp.ordem, cp.codigo";

        var contas = await conn.QueryAsync<ContaBalanco, ContaPadrao, ContaBalanco>(
            sqlContas,
            (cb, cp) =>
            {
                cb.ContaPadrao = cp;
                return cb;
            },
            new { EmpresaId = empresaId },
            splitOn: "Id"
        );

        foreach (var cb in contas)
        {
            if (porId.TryGetValue(cb.BalancoId, out var bal))
            {
                bal.Contas.Add(cb);
            }
        }

        return lista;
    }

    public async Task<bool> ExisteAsync(int empresaId, int anoExercicio, TipoBalanco tipo)
    {
        const string sql = @"
            SELECT COUNT(*) FROM balanco
             WHERE empresa_id=@e AND ano_exercicio=@a AND tipo_balanco=@t AND ativado=1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteScalarAsync<int>(sql, new { e = empresaId, a = anoExercicio, t = (byte)tipo }) > 0;
    }

    public async Task<Balanco?> BuscarPorHashPdfAsync(string hash)
    {
        const string sql = @"
            SELECT id AS Id, empresa_id AS EmpresaId, ano_exercicio AS AnoExercicio, 
                   data_referencia AS DataReferencia, tipo_balanco AS TipoBalanco, 
                   data_planilhamento AS DataPlanilhamento, usuario_id AS UsuarioId, 
                   origem AS Origem, hash_origem_pdf AS HashOrigemPdf, observacoes AS Observacoes, 
                   moeda AS Moeda, multiplicador_valores AS MultiplicadorValores
            FROM balanco WHERE hash_origem_pdf=@Hash AND ativado=1 LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Balanco>(sql, new { Hash = hash });
    }
}
