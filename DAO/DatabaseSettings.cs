using System.Reflection;
using System.Text.Json;

namespace BalancoPatrimonial.App.DAO;

/// <summary>
/// Configurações de conexão dos 3 bancos.
///
/// As credenciais NÃO ficam mais hardcoded: são lidas do arquivo
/// <c>appsettings.json</c> (embarcado no app) e podem ser sobrescritas por
/// VARIÁVEIS DE AMBIENTE — útil em produção, onde não se versiona segredo:
///   BALANCO_MYSQL_SERVER, BALANCO_MYSQL_PORT, BALANCO_MYSQL_DATABASE,
///   BALANCO_MYSQL_USER, BALANCO_MYSQL_PASSWORD,
///   BALANCO_MONGO_CONNECTIONSTRING, BALANCO_MONGO_DATABASE
///
/// Boa prática: em um cenário real, o appsettings.json versionado teria
/// apenas placeholders e o arquivo com segredos reais ficaria no .gitignore
/// ou seria provisionado por variável de ambiente.
/// </summary>
public class DatabaseSettings
{
    // ───── MySQL ─────
    public string MySqlServer { get; set; } = "127.0.0.1";
    public int MySqlPort { get; set; } = 3306;
    public string MySqlDatabase { get; set; } = "balanco_patrimonial";
    public string MySqlUser { get; set; } = "root";
    public string MySqlPassword { get; set; } = "";

    public string MySqlConnectionString =>
        $"Server={MySqlServer};Port={MySqlPort};Database={MySqlDatabase};" +
        $"User={MySqlUser};Password={MySqlPassword};SslMode=None;";

    // ───── SQLite (local) ─────
    public string SqliteFileName { get; set; } = "balanco_local.db3";
    public string SqliteFullPath => Path.Combine(FileSystem.AppDataDirectory, SqliteFileName);

    // ───── MongoDB ─────
    public string MongoConnectionString { get; set; } = "mongodb://127.0.0.1:27017";
    public string MongoDatabase { get; set; } = "balanco_logs";
    public string MongoCollectionLogs { get; set; } = "logs";
    public string MongoCollectionAnalisesIa { get; set; } = "analises_ia";

    // ───── Logs em TXT ─────
    public string PastaLogsTxt { get; set; } = "logs";
    public string CaminhoPastaLogsTxt => Path.Combine(FileSystem.AppDataDirectory, PastaLogsTxt);

    /// <summary>
    /// Carrega as configurações do appsettings.json embarcado e aplica
    /// overrides de variáveis de ambiente. Em caso de qualquer falha, mantém
    /// os defaults (o app não quebra na inicialização).
    /// </summary>
    public static DatabaseSettings CarregarPadroes()
    {
        var s = new DatabaseSettings();
        try
        {
            CarregarDoJson(s);
        }
        catch
        {
            // Mantém defaults — não impede o app de subir.
        }
        AplicarVariaveisDeAmbiente(s);
        return s;
    }

    private static void CarregarDoJson(DatabaseSettings s)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // O recurso embarcado fica como "<RootNamespace>.appsettings.json"
        var nome = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));
        if (nome is null) return;

        using var stream = assembly.GetManifestResourceStream(nome);
        if (stream is null) return;

        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        if (root.TryGetProperty("MySql", out var my))
        {
            s.MySqlServer   = Str(my, "Server", s.MySqlServer);
            s.MySqlPort     = my.TryGetProperty("Port", out var p) && p.TryGetInt32(out var pi) ? pi : s.MySqlPort;
            s.MySqlDatabase = Str(my, "Database", s.MySqlDatabase);
            s.MySqlUser     = Str(my, "User", s.MySqlUser);
            s.MySqlPassword = Str(my, "Password", s.MySqlPassword);
        }
        if (root.TryGetProperty("Sqlite", out var sq))
            s.SqliteFileName = Str(sq, "FileName", s.SqliteFileName);

        if (root.TryGetProperty("Mongo", out var mo))
        {
            s.MongoConnectionString    = Str(mo, "ConnectionString", s.MongoConnectionString);
            s.MongoDatabase            = Str(mo, "Database", s.MongoDatabase);
            s.MongoCollectionLogs      = Str(mo, "CollectionLogs", s.MongoCollectionLogs);
            s.MongoCollectionAnalisesIa = Str(mo, "CollectionAnalisesIa", s.MongoCollectionAnalisesIa);
        }
    }

    private static string Str(JsonElement obj, string prop, string fallback)
        => obj.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? fallback : fallback;

    private static void AplicarVariaveisDeAmbiente(DatabaseSettings s)
    {
        string? Env(string k) => Environment.GetEnvironmentVariable(k);

        if (Env("BALANCO_MYSQL_SERVER") is string srv) s.MySqlServer = srv;
        if (Env("BALANCO_MYSQL_PORT") is string pt && int.TryParse(pt, out var pti)) s.MySqlPort = pti;
        if (Env("BALANCO_MYSQL_DATABASE") is string db) s.MySqlDatabase = db;
        if (Env("BALANCO_MYSQL_USER") is string us) s.MySqlUser = us;
        if (Env("BALANCO_MYSQL_PASSWORD") is string pw) s.MySqlPassword = pw;
        if (Env("BALANCO_MONGO_CONNECTIONSTRING") is string mc) s.MongoConnectionString = mc;
        if (Env("BALANCO_MONGO_DATABASE") is string md) s.MongoDatabase = md;
    }
}
