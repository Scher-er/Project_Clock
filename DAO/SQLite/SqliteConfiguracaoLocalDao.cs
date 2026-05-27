using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;

namespace BalancoPatrimonial.App.DAO.SQLite;

/// <summary>
/// DAO de Configuração Local (SQLite). Lê e escreve preferências do app —
/// tema escuro/claro, último filtro, último usuário, etc.
///
/// Internamente usa <see cref="ConfiguracaoLocalSqlite"/> (com atributos
/// sqlite-net) mas a API externa é em <see cref="ConfiguracaoLocal"/> (POCO).
/// Isso desacopla a Model de domínio da biblioteca de persistência.
/// </summary>
public class SqliteConfiguracaoLocalDao : IConfiguracaoLocalDao
{
    private readonly ISqliteConnectionFactory _factory;
    public string Fonte => "SQLite.configuracao_local";

    public SqliteConfiguracaoLocalDao(ISqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<int> InserirAsync(ConfiguracaoLocal entidade)
    {
        var conn = await _factory.ObterConexaoAsync();
        var row = ParaSqlite(entidade);
        await conn.InsertAsync(row);
        entidade.Id = row.Id;
        return row.Id;
    }

    public async Task<bool> AtualizarAsync(ConfiguracaoLocal entidade)
    {
        var conn = await _factory.ObterConexaoAsync();
        var row = ParaSqlite(entidade);
        row.DataAlteracao = DateTime.Now;
        return await conn.UpdateAsync(row) > 0;
    }

    public async Task<bool> ExcluirAsync(int id)
    {
        var conn = await _factory.ObterConexaoAsync();
        return await conn.DeleteAsync<ConfiguracaoLocalSqlite>(id) > 0;
    }

    public async Task<ConfiguracaoLocal?> BuscarPorIdAsync(int id)
    {
        var conn = await _factory.ObterConexaoAsync();
        try
        {
            var row = await conn.GetAsync<ConfiguracaoLocalSqlite>(id);
            return DoSqlite(row);
        }
        catch (InvalidOperationException)
        {
            // sqlite-net lança quando não encontra
            return null;
        }
    }

    public async Task<IEnumerable<ConfiguracaoLocal>> ListarTodosAsync()
    {
        var conn = await _factory.ObterConexaoAsync();
        var rows = await conn.Table<ConfiguracaoLocalSqlite>().ToListAsync();
        return rows.Select(DoSqlite);
    }

    public async Task<string?> ObterValorAsync(string chave)
    {
        var conn = await _factory.ObterConexaoAsync();
        var row = await conn.Table<ConfiguracaoLocalSqlite>()
            .Where(c => c.Chave == chave)
            .FirstOrDefaultAsync();
        return row?.Valor;
    }

    public async Task DefinirValorAsync(string chave, string valor)
    {
        var conn = await _factory.ObterConexaoAsync();
        var existente = await conn.Table<ConfiguracaoLocalSqlite>()
            .Where(c => c.Chave == chave)
            .FirstOrDefaultAsync();

        if (existente is null)
        {
            await conn.InsertAsync(new ConfiguracaoLocalSqlite
            {
                Chave = chave,
                Valor = valor,
                DataAlteracao = DateTime.Now
            });
        }
        else
        {
            existente.Valor = valor;
            existente.DataAlteracao = DateTime.Now;
            await conn.UpdateAsync(existente);
        }
    }

    private static ConfiguracaoLocalSqlite ParaSqlite(ConfiguracaoLocal c) => new()
    {
        Id = c.Id,
        Chave = c.Chave,
        Valor = c.Valor,
        DataAlteracao = c.DataAlteracao
    };

    private static ConfiguracaoLocal DoSqlite(ConfiguracaoLocalSqlite r) => new()
    {
        Id = r.Id,
        Chave = r.Chave,
        Valor = r.Valor,
        DataAlteracao = r.DataAlteracao
    };
}
