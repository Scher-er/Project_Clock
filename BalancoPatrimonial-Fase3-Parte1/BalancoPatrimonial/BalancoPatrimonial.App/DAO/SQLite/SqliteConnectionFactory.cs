using BalancoPatrimonial.App.Models;
using SQLite;

namespace BalancoPatrimonial.App.DAO.SQLite;

/// <summary>
/// Fornece a conexão SQLite e garante que o schema local (tabela ConfiguracaoLocal)
/// existe. Singleton — a conexão SQLite async é segura pra reusar.
/// </summary>
public interface ISqliteConnectionFactory
{
    /// <summary>Retorna a conexão (criando o arquivo e o schema se necessário).</summary>
    Task<SQLiteAsyncConnection> ObterConexaoAsync();

    string CaminhoArquivo { get; }
}

public class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly DatabaseSettings _settings;
    private SQLiteAsyncConnection? _conexao;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    public string CaminhoArquivo => _settings.SqliteFullPath;

    public SqliteConnectionFactory(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<SQLiteAsyncConnection> ObterConexaoAsync()
    {
        if (_conexao is not null) return _conexao;

        await _semaforo.WaitAsync();
        try
        {
            if (_conexao is not null) return _conexao;

            // Garante que o diretório existe
            var diretorio = Path.GetDirectoryName(_settings.SqliteFullPath);
            if (!string.IsNullOrEmpty(diretorio) && !Directory.Exists(diretorio))
            {
                Directory.CreateDirectory(diretorio);
            }

            // SharedCache + ReadWrite + Create
            var flags = SQLiteOpenFlags.ReadWrite
                      | SQLiteOpenFlags.Create
                      | SQLiteOpenFlags.SharedCache;

            _conexao = new SQLiteAsyncConnection(_settings.SqliteFullPath, flags);

            // Cria a tabela se não existir (idempotente)
            await _conexao.CreateTableAsync<ConfiguracaoLocalSqlite>();

            return _conexao;
        }
        finally
        {
            _semaforo.Release();
        }
    }
}

/// <summary>
/// Versão da ConfiguracaoLocal com atributos do sqlite-net. Mantemos uma classe
/// separada pra não acoplar a Model de domínio à biblioteca de persistência.
/// O mapeamento entre as duas fica no SqliteConfiguracaoDao (Fase 3).
/// </summary>
[Table("configuracao_local")]
public class ConfiguracaoLocalSqlite
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true), MaxLength(50), NotNull]
    public string Chave { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Valor { get; set; } = string.Empty;

    public DateTime DataAlteracao { get; set; } = DateTime.Now;
}
