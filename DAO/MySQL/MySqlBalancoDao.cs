using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// DAO de Balanço. Caso especial: <see cref="InserirComContasAsync"/> insere
/// o balanço e todas as contas em UMA transação — se algo falhar, nada fica
/// gravado (evita o estado "balanço sem contas").
/// </summary>
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
                (@empresa, @ano, @data, @tipo,
                 @usuario, @origem, @hash, @obs,
                 @moeda, @mult);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        AdicionarParametros(cmd, b);
        b.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return b.Id;
    }

    public async Task<int> InserirComContasAsync(Balanco b)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            // 1) Insere o cabeçalho do balanço
            const string sqlBalanco = @"
                INSERT INTO balanco
                    (empresa_id, ano_exercicio, data_referencia, tipo_balanco,
                     usuario_id, origem, hash_origem_pdf, observacoes,
                     moeda, multiplicador_valores)
                VALUES
                    (@empresa, @ano, @data, @tipo,
                     @usuario, @origem, @hash, @obs,
                     @moeda, @mult);
                SELECT LAST_INSERT_ID();";

            await using (var cmd = new MySqlCommand(sqlBalanco, conn, tx))
            {
                AdicionarParametros(cmd, b);
                b.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // 2) Insere cada conta
            const string sqlConta = @"
                INSERT INTO conta_balanco
                    (balanco_id, conta_padrao_id, valor, descricao_original)
                VALUES
                    (@bal, @cp, @valor, @descOrig)";

            foreach (var cb in b.Contas)
            {
                cb.BalancoId = b.Id;
                await using var cmd = new MySqlCommand(sqlConta, conn, tx);
                cmd.Parameters.AddWithValue("@bal", b.Id);
                cmd.Parameters.AddWithValue("@cp", cb.ContaPadraoId);
                cmd.Parameters.AddWithValue("@valor", cb.Valor);
                cmd.Parameters.AddWithValue("@descOrig", (object?)cb.DescricaoOriginal ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

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
                ano_exercicio=@ano, data_referencia=@data, tipo_balanco=@tipo,
                origem=@origem, hash_origem_pdf=@hash, observacoes=@obs,
                moeda=@moeda, multiplicador_valores=@mult
            WHERE id=@id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", b.Id);
        AdicionarParametros(cmd, b);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        // SOFT DELETE: não remove do banco, apenas marca ativado=0. Assim o
        // analista pode recuperar manualmente no banco se foi engano.
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("UPDATE balanco SET ativado=0 WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    /// <summary>
    /// Retorna o id do balanço ATIVO (ativado=1) que casa com empresa+ano+tipo,
    /// ou null se não houver. Usado pela substituição.
    /// </summary>
    public async Task<int?> BuscarIdAtivoAsync(int empresaId, int anoExercicio, TipoBalanco tipo)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            @"SELECT id FROM balanco
              WHERE empresa_id=@e AND ano_exercicio=@a AND tipo_balanco=@t AND ativado=1
              LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        cmd.Parameters.AddWithValue("@a", anoExercicio);
        cmd.Parameters.AddWithValue("@t", (int)tipo);
        var r = await cmd.ExecuteScalarAsync();
        return r is null || r == DBNull.Value ? null : Convert.ToInt32(r);
    }

    /// <summary>
    /// Atualiza o cabeçalho E substitui todas as contas do balanço numa única
    /// transação (UPDATE header, DELETE contas antigas, INSERT contas novas).
    /// Usado pela tela de edição de um balanço já salvo.
    /// </summary>
    public async Task<bool> AtualizarComContasAsync(Balanco b)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            const string sqlHeader = @"
                UPDATE balanco SET
                    ano_exercicio=@ano, data_referencia=@data, tipo_balanco=@tipo,
                    origem=@origem, hash_origem_pdf=@hash, observacoes=@obs,
                    moeda=@moeda, multiplicador_valores=@mult
                WHERE id=@id";
            await using (var cmd = new MySqlCommand(sqlHeader, conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", b.Id);
                AdicionarParametros(cmd, b);
                await cmd.ExecuteNonQueryAsync();
            }

            await using (var del = new MySqlCommand("DELETE FROM conta_balanco WHERE balanco_id=@b", conn, tx))
            {
                del.Parameters.AddWithValue("@b", b.Id);
                await del.ExecuteNonQueryAsync();
            }

            const string sqlConta = @"
                INSERT INTO conta_balanco (balanco_id, conta_padrao_id, valor, descricao_original)
                VALUES (@bal, @cp, @valor, @descOrig)";
            foreach (var cb in b.Contas)
            {
                await using var cmd = new MySqlCommand(sqlConta, conn, tx);
                cmd.Parameters.AddWithValue("@bal", b.Id);
                cmd.Parameters.AddWithValue("@cp", cb.ContaPadraoId);
                cmd.Parameters.AddWithValue("@valor", cb.Valor);
                cmd.Parameters.AddWithValue("@descOrig", (object?)cb.DescricaoOriginal ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
            }

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
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM balanco WHERE id=@id AND ativado=1 LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<Balanco?> BuscarCompletoAsync(int balancoId)
    {
        var b = await BuscarPorIdAsync(balancoId);
        if (b is null) return null;

        const string sql = @"
            SELECT cb.id, cb.balanco_id, cb.conta_padrao_id, cb.valor, cb.descricao_original,
                   cp.id AS cp_id, cp.codigo, cp.descricao, cp.grupo_principal,
                   cp.conta_pai_id, cp.nivel, cp.eh_totalizadora, cp.ordem, cp.ativa
              FROM conta_balanco cb
              JOIN conta_padrao cp ON cp.id = cb.conta_padrao_id
             WHERE cb.balanco_id = @bal
             ORDER BY cp.ordem, cp.codigo";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@bal", balancoId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            b.Contas.Add(new ContaBalanco
            {
                Id = r.GetInt32("id"),
                BalancoId = r.GetInt32("balanco_id"),
                ContaPadraoId = r.GetInt32("conta_padrao_id"),
                Valor = r.GetDecimal("valor"),
                DescricaoOriginal = r.IsDBNull(r.GetOrdinal("descricao_original")) ? null : r.GetString("descricao_original"),
                ContaPadrao = new ContaPadrao
                {
                    Id = r.GetInt32("cp_id"),
                    Codigo = r.GetString("codigo"),
                    Descricao = r.GetString("descricao"),
                    GrupoPrincipal = (GrupoContaPrincipal)r.GetByte("grupo_principal"),
                    ContaPaiId = r.IsDBNull(r.GetOrdinal("conta_pai_id")) ? null : r.GetInt32("conta_pai_id"),
                    Nivel = r.GetByte("nivel"),
                    EhTotalizadora = r.GetBoolean("eh_totalizadora"),
                    Ordem = r.GetInt32("ordem"),
                    Ativa = r.GetBoolean("ativa")
                }
            });
        }
        return b;
    }

    public async Task<IEnumerable<Balanco>> ListarTodosAsync()
    {
        var lista = new List<Balanco>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "SELECT * FROM balanco WHERE ativado=1 ORDER BY ano_exercicio DESC, empresa_id", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    public async Task<IEnumerable<Balanco>> ListarPorEmpresaAsync(int empresaId)
    {
        var lista = new List<Balanco>();
        await using var conn = await _connFactory.AbrirConexaoAsync();

        // 1) Cabeçalhos dos balanços
        await using (var cmd = new MySqlCommand(
            "SELECT * FROM balanco WHERE empresa_id=@e AND ativado=1 ORDER BY ano_exercicio DESC", conn))
        {
            cmd.Parameters.AddWithValue("@e", empresaId);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) lista.Add(Mapear(r));
        }

        if (lista.Count == 0) return lista;

        // 2) Carrega TODAS as contas de TODOS os balanços da empresa numa query só
        //    e distribui por balanco_id. Sem isso, AtivoTotal/contagem vinham zerados
        //    na tela de visualização (as contas não eram trazidas do banco).
        var porId = lista.ToDictionary(b => b.Id, b => b);
        const string sqlContas = @"
            SELECT cb.id, cb.balanco_id, cb.conta_padrao_id, cb.valor, cb.descricao_original,
                   cp.id AS cp_id, cp.codigo, cp.descricao, cp.grupo_principal,
                   cp.conta_pai_id, cp.nivel, cp.eh_totalizadora, cp.ordem, cp.ativa
              FROM conta_balanco cb
              JOIN conta_padrao cp ON cp.id = cb.conta_padrao_id
              JOIN balanco b      ON b.id  = cb.balanco_id
             WHERE b.empresa_id = @e
             ORDER BY cp.ordem, cp.codigo";

        await using (var cmd = new MySqlCommand(sqlContas, conn))
        {
            cmd.Parameters.AddWithValue("@e", empresaId);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var balId = r.GetInt32("balanco_id");
                if (!porId.TryGetValue(balId, out var bal)) continue;

                bal.Contas.Add(new ContaBalanco
                {
                    Id = r.GetInt32("id"),
                    BalancoId = balId,
                    ContaPadraoId = r.GetInt32("conta_padrao_id"),
                    Valor = r.GetDecimal("valor"),
                    DescricaoOriginal = r.IsDBNull(r.GetOrdinal("descricao_original")) ? null : r.GetString("descricao_original"),
                    ContaPadrao = new ContaPadrao
                    {
                        Id = r.GetInt32("cp_id"),
                        Codigo = r.GetString("codigo"),
                        Descricao = r.GetString("descricao"),
                        GrupoPrincipal = (GrupoContaPrincipal)r.GetByte("grupo_principal"),
                        ContaPaiId = r.IsDBNull(r.GetOrdinal("conta_pai_id")) ? null : r.GetInt32("conta_pai_id"),
                        Nivel = r.GetByte("nivel"),
                        EhTotalizadora = r.GetBoolean("eh_totalizadora"),
                        Ordem = r.GetInt32("ordem"),
                        Ativa = r.GetBoolean("ativa")
                    }
                });
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
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        cmd.Parameters.AddWithValue("@a", anoExercicio);
        cmd.Parameters.AddWithValue("@t", (byte)tipo);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<Balanco?> BuscarPorHashPdfAsync(string hash)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "SELECT * FROM balanco WHERE hash_origem_pdf=@h AND ativado=1 LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@h", hash);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    private static void AdicionarParametros(MySqlCommand cmd, Balanco b)
    {
        cmd.Parameters.AddWithValue("@empresa", b.EmpresaId);
        cmd.Parameters.AddWithValue("@ano", b.AnoExercicio);
        cmd.Parameters.AddWithValue("@data", b.DataReferencia);
        cmd.Parameters.AddWithValue("@tipo", (byte)b.TipoBalanco);
        cmd.Parameters.AddWithValue("@usuario", b.UsuarioId);
        cmd.Parameters.AddWithValue("@origem", (object?)b.Origem ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hash", (object?)b.HashOrigemPdf ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@obs", (object?)b.Observacoes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@moeda", b.Moeda);
        cmd.Parameters.AddWithValue("@mult", b.MultiplicadorValores);
    }

    private static Balanco Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        EmpresaId = r.GetInt32("empresa_id"),
        AnoExercicio = r.GetInt32("ano_exercicio"),
        DataReferencia = r.GetDateTime("data_referencia"),
        TipoBalanco = (TipoBalanco)r.GetByte("tipo_balanco"),
        DataPlanilhamento = r.GetDateTime("data_planilhamento"),
        UsuarioId = r.GetInt32("usuario_id"),
        Origem = r.IsDBNull(r.GetOrdinal("origem")) ? null : r.GetString("origem"),
        HashOrigemPdf = r.IsDBNull(r.GetOrdinal("hash_origem_pdf")) ? null : r.GetString("hash_origem_pdf"),
        Observacoes = r.IsDBNull(r.GetOrdinal("observacoes")) ? null : r.GetString("observacoes"),
        Moeda = r.GetString("moeda"),
        MultiplicadorValores = r.GetInt32("multiplicador_valores")
    };
}
