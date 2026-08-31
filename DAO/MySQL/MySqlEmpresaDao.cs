using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using BalancoPatrimonial.App.Models.Enums;
using Dapper;

namespace BalancoPatrimonial.App.DAO.MySQL;

/// <summary>
/// DAO de Empresa usando Dapper para alta performance e menos boilerplate.
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
                (@Cnpj, @RazaoSocial, @NomeFantasia, @GrupoEconomicoId, @TipoEmpresa,
                 @Rating, @LimiteCredito, @UfAtuacao, @LocalAtuacao, @CaminhoLogo, @Ativo);
            SELECT LAST_INSERT_ID();";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        e.Id = await conn.ExecuteScalarAsync<int>(sql, e);
        return e.Id;
    }

    public async Task<bool> AtualizarAsync(Empresa e)
    {
        const string sql = @"
            UPDATE empresa SET
                cnpj=@Cnpj, razao_social=@RazaoSocial, nome_fantasia=@NomeFantasia,
                grupo_economico_id=@GrupoEconomicoId, tipo_empresa=@TipoEmpresa,
                rating=@Rating, limite_credito=@LimiteCredito,
                uf_atuacao=@UfAtuacao, local_atuacao=@LocalAtuacao,
                caminho_logo=@CaminhoLogo, ativo=@Ativo
            WHERE id=@Id";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(sql, e) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync("UPDATE empresa SET ativado=0 WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<Empresa?> BuscarPorIdAsync(int id)
    {
        const string sql = @"
            SELECT id AS Id, cnpj AS Cnpj, razao_social AS RazaoSocial, nome_fantasia AS NomeFantasia,
                   grupo_economico_id AS GrupoEconomicoId, tipo_empresa AS TipoEmpresa, rating AS Rating,
                   limite_credito AS LimiteCredito, uf_atuacao AS UfAtuacao, local_atuacao AS LocalAtuacao,
                   caminho_logo AS CaminhoLogo, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM empresa WHERE id=@Id LIMIT 1";
        
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Empresa>(sql, new { Id = id });
    }

    public async Task<Empresa?> BuscarPorCnpjAsync(string cnpj)
    {
        const string sql = @"
            SELECT id AS Id, cnpj AS Cnpj, razao_social AS RazaoSocial, nome_fantasia AS NomeFantasia,
                   grupo_economico_id AS GrupoEconomicoId, tipo_empresa AS TipoEmpresa, rating AS Rating,
                   limite_credito AS LimiteCredito, uf_atuacao AS UfAtuacao, local_atuacao AS LocalAtuacao,
                   caminho_logo AS CaminhoLogo, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM empresa WHERE cnpj=@Cnpj LIMIT 1";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryFirstOrDefaultAsync<Empresa>(sql, new { Cnpj = cnpj });
    }

    public async Task<Empresa?> BuscarCompletaAsync(int empresaId)
    {
        var empresa = await BuscarPorIdAsync(empresaId);
        if (empresa is null) return null;

        await using var conn = await _connFactory.AbrirConexaoAsync();

        if (empresa.GrupoEconomicoId.HasValue)
        {
            const string sqlGrupo = "SELECT id AS Id, nome AS Nome, descricao AS Descricao, data_cadastro AS DataCadastro, ativo AS Ativo FROM grupo_economico WHERE id=@Id";
            empresa.GrupoEconomico = await conn.QueryFirstOrDefaultAsync<GrupoEconomico>(sqlGrupo, new { Id = empresa.GrupoEconomicoId.Value });
        }

        empresa.Setores = (await CarregarSetoresAsync(conn, empresaId)).ToList();
        empresa.Balancos = (await CarregarBalancosResumoAsync(conn, empresaId)).ToList();

        return empresa;
    }

    public async Task<IEnumerable<Empresa>> ListarTodosAsync()
    {
        const string sql = @"
            SELECT id AS Id, cnpj AS Cnpj, razao_social AS RazaoSocial, nome_fantasia AS NomeFantasia,
                   grupo_economico_id AS GrupoEconomicoId, tipo_empresa AS TipoEmpresa, rating AS Rating,
                   limite_credito AS LimiteCredito, uf_atuacao AS UfAtuacao, local_atuacao AS LocalAtuacao,
                   caminho_logo AS CaminhoLogo, data_cadastro AS DataCadastro, ativo AS Ativo
            FROM empresa WHERE ativado=1 ORDER BY razao_social";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.QueryAsync<Empresa>(sql);
    }

    public async Task<IEnumerable<Empresa>> ListarComResumoAsync()
    {
        const string sql = @"
            SELECT e.id AS Id, e.cnpj AS Cnpj, e.razao_social AS RazaoSocial, e.nome_fantasia AS NomeFantasia,
                   e.grupo_economico_id AS GrupoEconomicoId, e.tipo_empresa AS TipoEmpresa, e.rating AS Rating,
                   e.limite_credito AS LimiteCredito, e.uf_atuacao AS UfAtuacao, e.local_atuacao AS LocalAtuacao,
                   e.caminho_logo AS CaminhoLogo, e.data_cadastro AS DataCadastro, e.ativo AS Ativo,
                   g.nome AS grupo_nome
              FROM empresa e
              LEFT JOIN grupo_economico g ON g.id = e.grupo_economico_id
             WHERE e.ativado = 1
             ORDER BY e.razao_social";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        
        var empresas = (await conn.QueryAsync<Empresa, string, Empresa>(
            sql, 
            (e, grupoNome) => 
            {
                if (!string.IsNullOrEmpty(grupoNome))
                {
                    e.GrupoEconomico = new GrupoEconomico { Id = e.GrupoEconomicoId ?? 0, Nome = grupoNome };
                }
                return e;
            },
            splitOn: "grupo_nome"
        )).ToList();

        foreach (var e in empresas)
        {
            e.Setores = (await CarregarSetoresAsync(conn, e.Id)).ToList();
            e.Balancos = (await CarregarBalancosResumoAsync(conn, e.Id)).ToList();
        }
        return empresas;
    }

    public async Task<IEnumerable<Empresa>> PesquisarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await ListarComResumoAsync();

        const string sql = @"
            SELECT DISTINCT e.id AS Id, e.cnpj AS Cnpj, e.razao_social AS RazaoSocial, e.nome_fantasia AS NomeFantasia,
                   e.grupo_economico_id AS GrupoEconomicoId, e.tipo_empresa AS TipoEmpresa, e.rating AS Rating,
                   e.limite_credito AS LimiteCredito, e.uf_atuacao AS UfAtuacao, e.local_atuacao AS LocalAtuacao,
                   e.caminho_logo AS CaminhoLogo, e.data_cadastro AS DataCadastro, e.ativo AS Ativo,
                   g.nome AS grupo_nome
              FROM empresa e
              LEFT JOIN grupo_economico g ON g.id = e.grupo_economico_id
             WHERE e.ativado = 1
               AND (e.razao_social  LIKE @t
                OR e.nome_fantasia LIKE @t
                OR e.cnpj          LIKE @t
                OR g.nome          LIKE @t)
             ORDER BY e.razao_social
             LIMIT 200";

        await using var conn = await _connFactory.AbrirConexaoAsync();
        var empresas = (await conn.QueryAsync<Empresa, string, Empresa>(
            sql,
            (e, grupoNome) =>
            {
                if (!string.IsNullOrEmpty(grupoNome))
                {
                    e.GrupoEconomico = new GrupoEconomico { Id = e.GrupoEconomicoId ?? 0, Nome = grupoNome };
                }
                return e;
            },
            new { t = $"%{termo}%" },
            splitOn: "grupo_nome"
        )).ToList();

        foreach (var e in empresas)
        {
            e.Setores = (await CarregarSetoresAsync(conn, e.Id)).ToList();
            e.Balancos = (await CarregarBalancosResumoAsync(conn, e.Id)).ToList();
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
        await conn.ExecuteAsync(sql, new { e = empresaId, s = setorId, p = principal });
    }

    public async Task<bool> DesvincularSetorAsync(int empresaId, int setorId)
    {
        await using var conn = await _connFactory.AbrirConexaoAsync();
        return await conn.ExecuteAsync(
            "DELETE FROM empresa_setor WHERE empresa_id=@e AND setor_id=@s", 
            new { e = empresaId, s = setorId }) > 0;
    }

    private async Task<IEnumerable<SetorAtividade>> CarregarSetoresAsync(System.Data.IDbConnection conn, int empresaId)
    {
        const string sql = @"
            SELECT s.id AS Id, s.codigo AS Codigo, s.nome AS Nome, s.descricao AS Descricao
              FROM setor_atividade s
              JOIN empresa_setor es ON es.setor_id = s.id
             WHERE es.empresa_id = @Id
             ORDER BY es.principal DESC, s.codigo";

        return await conn.QueryAsync<SetorAtividade>(sql, new { Id = empresaId });
    }

    private async Task<IEnumerable<Balanco>> CarregarBalancosResumoAsync(System.Data.IDbConnection conn, int empresaId)
    {
        const string sql = @"
            SELECT id AS Id, ano_exercicio AS AnoExercicio, data_referencia AS DataReferencia, 
                   tipo_balanco AS TipoBalanco, data_planilhamento AS DataPlanilhamento, moeda AS Moeda,
                   empresa_id AS EmpresaId
              FROM balanco
             WHERE empresa_id = @Id
             ORDER BY ano_exercicio DESC, tipo_balanco";

        return await conn.QueryAsync<Balanco>(sql, new { Id = empresaId });
    }
}
