using System.Data;
using Dapper;
using MySqlConnector;
using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.MySQL;

public class MySqlMapeamentoDeParaDao : IMapeamentoDeParaDao
{
    public string Fonte => "MySQL.mapeamentos_de_para";
    private readonly string _connectionString;

    public MySqlMapeamentoDeParaDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    private MySqlConnection CreateConnection() => new(_connectionString);

    public async Task<MapeamentoDePara?> BuscarMapeamentoAsync(string textoOriginal, int? empresaId)
    {
        using var conexao = CreateConnection();
        var sql = @"
            SELECT m.*, c.*
            FROM MapeamentosDePara m
            INNER JOIN ContasPadrao c ON m.ContaPadraoId = c.Id
            WHERE m.TextoOriginal = @TextoOriginal
              AND (m.EmpresaId = @EmpresaId OR m.EmpresaId IS NULL)
            ORDER BY m.EmpresaId DESC
            LIMIT 1;
        ";
        
        var mappings = await conexao.QueryAsync<MapeamentoDePara, ContaPadrao, MapeamentoDePara>(
            sql,
            (map, conta) =>
            {
                map.ContaPadrao = conta;
                return map;
            },
            new { TextoOriginal = textoOriginal.ToLowerInvariant().Trim(), EmpresaId = empresaId }
        );
        return mappings.FirstOrDefault();
    }

    public async Task<IEnumerable<MapeamentoDePara>> ListarGlobalAsync()
    {
        using var conexao = CreateConnection();
        var sql = @"
            SELECT m.*, c.*
            FROM MapeamentosDePara m
            INNER JOIN ContasPadrao c ON m.ContaPadraoId = c.Id
            WHERE m.EmpresaId IS NULL;
        ";
        return await conexao.QueryAsync<MapeamentoDePara, ContaPadrao, MapeamentoDePara>(
            sql,
            (map, conta) => { map.ContaPadrao = conta; return map; }
        );
    }

    public async Task<IEnumerable<MapeamentoDePara>> ListarPorEmpresaAsync(int empresaId)
    {
        using var conexao = CreateConnection();
        var sql = @"
            SELECT m.*, c.*
            FROM MapeamentosDePara m
            INNER JOIN ContasPadrao c ON m.ContaPadraoId = c.Id
            WHERE m.EmpresaId = @EmpresaId;
        ";
        return await conexao.QueryAsync<MapeamentoDePara, ContaPadrao, MapeamentoDePara>(
            sql,
            (map, conta) => { map.ContaPadrao = conta; return map; },
            new { EmpresaId = empresaId }
        );
    }

    public async Task<int> InserirAsync(MapeamentoDePara entidade)
    {
        using var conexao = CreateConnection();
        var sql = @"
            INSERT INTO MapeamentosDePara (TextoOriginal, ContaPadraoId, EmpresaId)
            VALUES (@TextoOriginal, @ContaPadraoId, @EmpresaId);
            SELECT LAST_INSERT_ID();
        ";
        entidade.Id = await conexao.ExecuteScalarAsync<int>(sql, new {
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
        return entidade.Id;
    }

    public async Task<bool> AtualizarAsync(MapeamentoDePara entidade)
    {
        using var conexao = CreateConnection();
        var sql = @"
            UPDATE MapeamentosDePara
            SET TextoOriginal = @TextoOriginal,
                ContaPadraoId = @ContaPadraoId,
                EmpresaId = @EmpresaId
            WHERE Id = @Id;
        ";
        var rows = await conexao.ExecuteAsync(sql, new {
            entidade.Id,
            TextoOriginal = entidade.TextoOriginal.ToLowerInvariant().Trim(),
            entidade.ContaPadraoId,
            entidade.EmpresaId
        });
        return rows > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        using var conexao = CreateConnection();
        var rows = await conexao.ExecuteAsync("DELETE FROM MapeamentosDePara WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }

    public Task<MapeamentoDePara?> BuscarPorIdAsync(int id) => throw new NotImplementedException();
    public Task<IEnumerable<MapeamentoDePara>> ListarTodosAsync() => throw new NotImplementedException();
}
