using MongoDB.Driver;

namespace BalancoPatrimonial.App.DAO.Mongo;

/// <summary>
/// Fornece a collection de logs do MongoDB. O driver oficial é thread-safe e
/// faz pooling internamente, então mantemos um único client.
/// </summary>
public interface IMongoConnectionFactory
{
    IMongoDatabase ObterDatabase();
    IMongoCollection<TDocumento> ObterCollection<TDocumento>(string nomeCollection);

    /// <summary>Testa conectividade com um ping rápido.</summary>
    Task<bool> TestarConexaoAsync(CancellationToken ct = default);
}

public class MongoConnectionFactory : IMongoConnectionFactory
{
    private readonly DatabaseSettings _settings;
    private readonly Lazy<IMongoClient> _client;

    public MongoConnectionFactory(DatabaseSettings settings)
    {
        _settings = settings;
        _client = new Lazy<IMongoClient>(() =>
        {
            // Timeout curto: se o servidor Mongo não estiver no ar, falha em ~3s
            // em vez dos 30s padrão — evita travar a UI a cada log.
            var cfg = MongoClientSettings.FromConnectionString(_settings.MongoConnectionString);
            cfg.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            cfg.ConnectTimeout = TimeSpan.FromSeconds(3);
            return new MongoClient(cfg);
        });
    }

    public IMongoDatabase ObterDatabase()
        => _client.Value.GetDatabase(_settings.MongoDatabase);

    public IMongoCollection<TDocumento> ObterCollection<TDocumento>(string nomeCollection)
        => ObterDatabase().GetCollection<TDocumento>(nomeCollection);

    public async Task<bool> TestarConexaoAsync(CancellationToken ct = default)
    {
        try
        {
            var db = ObterDatabase();
            await db.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                new MongoDB.Bson.BsonDocument("ping", 1), cancellationToken: ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
