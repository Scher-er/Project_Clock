namespace BalancoPatrimonial.App.DAO;

/// <summary>
/// Configurações de conexão dos 3 bancos. Os valores aqui são DEFAULTS pra desenvolvimento local.
///
/// Em produção (e quem for clonar o repo) deve sobrescrever via:
///   1. Variáveis de ambiente, OU
///   2. Edição direta no método <see cref="CarregarPadroes"/>
///
/// NÃO comitar credenciais reais no Git.
/// </summary>
public class DatabaseSettings
{
    // ───── MySQL ─────
    public string MySqlServer { get; set; } = "localhost";
    public int MySqlPort { get; set; } = 3306;
    public string MySqlDatabase { get; set; } = "balanco_patrimonial";
    public string MySqlUser { get; set; } = "root";
    public string MySqlPassword { get; set; } = "root";

    public string MySqlConnectionString =>
        $"Server={MySqlServer};Port={MySqlPort};Database={MySqlDatabase};" +
        $"User={MySqlUser};Password={MySqlPassword};SslMode=None;";

    // ───── SQLite (local) ─────
    /// <summary>Nome do arquivo. Será criado em FileSystem.AppDataDirectory.</summary>
    public string SqliteFileName { get; set; } = "balanco_local.db3";

    public string SqliteFullPath =>
        Path.Combine(FileSystem.AppDataDirectory, SqliteFileName);

    // ───── MongoDB ─────
    public string MongoConnectionString { get; set; } = "mongodb://localhost:27017";
    public string MongoDatabase { get; set; } = "balanco_logs";
    public string MongoCollectionLogs { get; set; } = "logs";

    // ───── Logs em TXT (replicação local em arquivo diário) ─────
    /// <summary>Nome da subpasta dentro de FileSystem.AppDataDirectory onde ficam os logs/YYYY-MM-DD.txt.</summary>
    public string PastaLogsTxt { get; set; } = "logs";

    public string CaminhoPastaLogsTxt =>
        Path.Combine(FileSystem.AppDataDirectory, PastaLogsTxt);

    /// <summary>
    /// Carrega defaults. No futuro substituir por leitura de arquivo de configuração
    /// (appsettings.json embarcado como MauiAsset).
    /// </summary>
    public static DatabaseSettings CarregarPadroes() => new();
}
