using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using MySqlConnector;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// DAO de Empresa. É o mais complexo do sistema porque Empresa tem 3 navegações:
///   - 1 grupo_economico (FK direta)
///   - N setores (via empresa_setor — N:N)
///   - N balanços (FK reversa)
///
/// Em vez de um único método "BuscarTudo" gigante, expomos métodos com nomes
/// diferentes deixando explícito o que cada um carrega. A View escolhe o mais
/// barato pro seu caso de uso.
/// </summary>
public class MySqlEmpresaDao : IEmpresaDao
{
    private readonly IMySqlConnectionFactory _connFactory;
    public string Fonte => "MySQL.empresa";

    public MySqlEmpresaDao(IMySqlConnectionFactory connFactory)
    {
        _connFactory = connFactory;
    }

    public async Task<int> InserirAsync(Empresa e)
    {
        const string sql = @"
            INSERT INTO empresa
                (cnpj, razao_social, nome_fantasia, grupo_economico_id, tipo_empresa,
                 rating, limite_credito, uf_atuacao, local_atuacao, caminho_logo, ativo)
            VALUES
                (@cnpj, @razao, @fantasia, @grupo, @tipo,
                 @rating, @limite, @uf, @local, @logo, @ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        AdicionarParametros(cmd, e);

        e.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return e.Id;
    }

    public async Task<bool> AtualizarAsync(Empresa e)
    {
        const string sql = @"
            UPDATE empresa SET
                cnpj=@cnpj, razao_social=@razao, nome_fantasia=@fantasia,
                grupo_economico_id=@grupo, tipo_empresa=@tipo,
                rating=@rating, limite_credito=@limite,
                uf_atuacao=@uf, local_atuacao=@local,
                caminho_logo=@logo, ativo=@ativo
            WHERE id=@id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", e.Id);
        AdicionarParametros(cmd, e);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("UPDATE empresa SET ativado=0 WHERE id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<Empresa?> BuscarPorIdAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM empresa WHERE id=@id LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<Empresa?> BuscarPorCnpjAsync(string cnpj)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM empresa WHERE cnpj=@cnpj LIMIT 1", conn);
        cmd.Parameters.AddWithValue("@cnpj", cnpj);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Mapear(r) : null;
    }

    public async Task<Empresa?> BuscarCompletaAsync(int empresaId)
    {
        var empresa = await BuscarPorIdAsync(empresaId);
        if (empresa is null) return null;

        // Grupo (se houver)
        if (empresa.GrupoEconomicoId.HasValue)
        {
            await using var conn = await _connFactory.AbrirConexaoAsync();
            await using var cmd = new MySqlCommand("SELECT * FROM grupo_economico WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", empresa.GrupoEconomicoId.Value);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                empresa.GrupoEconomico = new GrupoEconomico
                {
                    Id = r.GetInt32("id"),
                    Nome = r.GetString("nome"),
                    Descricao = r.IsDBNull(r.GetOrdinal("descricao")) ? null : r.GetString("descricao"),
                    DataCadastro = r.GetDateTime("data_cadastro"),
                    Ativo = r.GetBoolean("ativo")
                };
            }
        }

        // Setores (N:N)
        empresa.Setores = (await CarregarSetoresAsync(empresaId)).ToList();

        // Balanços (resumo — sem contas pra não pesar)
        empresa.Balancos = (await CarregarBalancosResumoAsync(empresaId)).ToList();

        return empresa;
    }

    public async Task<IEnumerable<Empresa>> ListarTodosAsync()
    {
        var lista = new List<Empresa>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand("SELECT * FROM empresa WHERE ativado=1 ORDER BY razao_social", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(Mapear(r));
        return lista;
    }

    public async Task<IEnumerable<Empresa>> ListarComResumoAsync()
    {
        // Estratégia: 1 query principal pra empresas+grupos, e queries auxiliares
        // pra carregar setores e anos dos balanços em batch.
        const string sql = @"
            SELECT e.*, g.nome AS grupo_nome
              FROM empresa e
              LEFT JOIN grupo_economico g ON g.id = e.grupo_economico_id
             WHERE e.ativado = 1
             ORDER BY e.razao_social";

        var empresas = new List<Empresa>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using (var cmd = new MySqlCommand(sql, conn))
        await using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                var e = Mapear(r);
                if (!r.IsDBNull(r.GetOrdinal("grupo_nome")))
                {
                    e.GrupoEconomico = new GrupoEconomico
                    {
                        Id = e.GrupoEconomicoId ?? 0,
                        Nome = r.GetString("grupo_nome")
                    };
                }
                empresas.Add(e);
            }
        }

        // Carrega setores e anos pra cada empresa (loop simples — para listas
        // pequenas é OK; pra grandes, otimizar com WHERE empresa_id IN (...))
        foreach (var e in empresas)
        {
            e.Setores = (await CarregarSetoresAsync(e.Id)).ToList();
            e.Balancos = (await CarregarBalancosResumoAsync(e.Id)).ToList();
        }
        return empresas;
    }

    public async Task<IEnumerable<Empresa>> PesquisarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await ListarComResumoAsync();

        const string sql = @"
            SELECT DISTINCT e.*, g.nome AS grupo_nome
              FROM empresa e
              LEFT JOIN grupo_economico g ON g.id = e.grupo_economico_id
             WHERE e.ativado = 1
               AND (e.razao_social  LIKE @t
                OR e.nome_fantasia LIKE @t
                OR e.cnpj          LIKE @t
                OR g.nome          LIKE @t)
             ORDER BY e.razao_social
             LIMIT 200";

        var empresas = new List<Empresa>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using (var cmd = new MySqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@t", $"%{termo}%");
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var e = Mapear(r);
                if (!r.IsDBNull(r.GetOrdinal("grupo_nome")))
                {
                    e.GrupoEconomico = new GrupoEconomico
                    {
                        Id = e.GrupoEconomicoId ?? 0,
                        Nome = r.GetString("grupo_nome")
                    };
                }
                empresas.Add(e);
            }
        }
        foreach (var e in empresas)
        {
            e.Setores = (await CarregarSetoresAsync(e.Id)).ToList();
            e.Balancos = (await CarregarBalancosResumoAsync(e.Id)).ToList();
        }
        return empresas;
    }

    public async Task VincularSetorAsync(int empresaId, int setorId, bool principal)
    {
        const string sql = @"
            INSERT INTO empresa_setor (empresa_id, setor_id, principal)
            VALUES (@e, @s, @p)
            ON DUPLICATE KEY UPDATE principal=@p";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        cmd.Parameters.AddWithValue("@s", setorId);
        cmd.Parameters.AddWithValue("@p", principal);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> DesvincularSetorAsync(int empresaId, int setorId)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(
            "DELETE FROM empresa_setor WHERE empresa_id=@e AND setor_id=@s", conn);
        cmd.Parameters.AddWithValue("@e", empresaId);
        cmd.Parameters.AddWithValue("@s", setorId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ───── helpers privados ───────────────────────────────────────────────────

    private async Task<IEnumerable<SetorAtividade>> CarregarSetoresAsync(int empresaId)
    {
        const string sql = @"
            SELECT s.*
              FROM setor_atividade s
              JOIN empresa_setor es ON es.setor_id = s.id
             WHERE es.empresa_id = @id
             ORDER BY es.principal DESC, s.codigo";

        var lista = new List<SetorAtividade>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", empresaId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new SetorAtividade
            {
                Id = r.GetInt32("id"),
                Codigo = r.GetString("codigo"),
                Nome = r.GetString("nome"),
                Descricao = r.IsDBNull(r.GetOrdinal("descricao")) ? null : r.GetString("descricao")
            });
        }
        return lista;
    }

    private async Task<IEnumerable<Balanco>> CarregarBalancosResumoAsync(int empresaId)
    {
        // Apenas dados básicos do balanço (sem contas) — usado pra mostrar
        // "balanços planilhados: 2020, 2021, 2022" na lista de empresas.
        const string sql = @"
            SELECT id, ano_exercicio, data_referencia, tipo_balanco,
                   data_planilhamento, moeda
              FROM balanco
             WHERE empresa_id = @id
             ORDER BY ano_exercicio DESC, tipo_balanco";

        var lista = new List<Balanco>();
        await using var conn = await _connFactory.AbrirConexaoAsync();
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", empresaId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            lista.Add(new Balanco
            {
                Id = r.GetInt32("id"),
                EmpresaId = empresaId,
                AnoExercicio = r.GetInt32("ano_exercicio"),
                DataReferencia = r.GetDateTime("data_referencia"),
                TipoBalanco = (TipoBalanco)r.GetByte("tipo_balanco"),
                DataPlanilhamento = r.GetDateTime("data_planilhamento"),
                Moeda = r.GetString("moeda")
            });
        }
        return lista;
    }

    private static void AdicionarParametros(MySqlCommand cmd, Empresa e)
    {
        cmd.Parameters.AddWithValue("@cnpj", e.Cnpj);
        cmd.Parameters.AddWithValue("@razao", e.RazaoSocial);
        cmd.Parameters.AddWithValue("@fantasia", (object?)e.NomeFantasia ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@grupo", (object?)e.GrupoEconomicoId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tipo", (byte)e.TipoEmpresa);
        cmd.Parameters.AddWithValue("@rating", (object?)e.Rating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@limite", (object?)e.LimiteCredito ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@uf", (object?)e.UfAtuacao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@local", (object?)e.LocalAtuacao ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@logo", (object?)e.CaminhoLogo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ativo", e.Ativo);
    }

    private static Empresa Mapear(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        Cnpj = r.GetString("cnpj"),
        RazaoSocial = r.GetString("razao_social"),
        NomeFantasia = r.IsDBNull(r.GetOrdinal("nome_fantasia")) ? null : r.GetString("nome_fantasia"),
        GrupoEconomicoId = r.IsDBNull(r.GetOrdinal("grupo_economico_id")) ? null : r.GetInt32("grupo_economico_id"),
        TipoEmpresa = (TipoEmpresa)r.GetByte("tipo_empresa"),
        Rating = r.IsDBNull(r.GetOrdinal("rating")) ? null : r.GetString("rating"),
        LimiteCredito = r.IsDBNull(r.GetOrdinal("limite_credito")) ? null : r.GetDecimal("limite_credito"),
        UfAtuacao = r.IsDBNull(r.GetOrdinal("uf_atuacao")) ? null : r.GetString("uf_atuacao"),
        LocalAtuacao = r.IsDBNull(r.GetOrdinal("local_atuacao")) ? null : r.GetString("local_atuacao"),
        CaminhoLogo = r.IsDBNull(r.GetOrdinal("caminho_logo")) ? null : r.GetString("caminho_logo"),
        DataCadastro = r.GetDateTime("data_cadastro"),
        Ativo = r.GetBoolean("ativo")
    };
}
