using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace BalancoPatrimonial.App.DAO.Mongo;

/// <summary>
/// Guarda as respostas brutas da IA no MongoDB (coleção "analises_ia").
/// Tolerante a falhas: se o Mongo estiver fora, não atrapalha a análise.
/// </summary>
public class MongoAnaliseIaDao : IAnaliseIaDao
{
    private readonly IMongoCollection<AnaliseIaBruta> _collection;

    static MongoAnaliseIaDao()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(AnaliseIaBruta)))
        {
            BsonClassMap.RegisterClassMap<AnaliseIaBruta>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(x => x.Id)
                  .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance)
                  .SetSerializer(new MongoDB.Bson.Serialization.Serializers.StringSerializer(BsonType.ObjectId));
            });
        }
    }

    public MongoAnaliseIaDao(IMongoConnectionFactory factory, DatabaseSettings settings)
    {
        _collection = factory.ObterCollection<AnaliseIaBruta>(settings.MongoCollectionAnalisesIa);
        _ = CriarIndicesAsync();
    }

    private async Task CriarIndicesAsync()
    {
        try
        {
            var keys = Builders<AnaliseIaBruta>.IndexKeys
                .Ascending(x => x.HashPdf).Descending(x => x.DataHora);
            await _collection.Indexes.CreateOneAsync(new CreateIndexModel<AnaliseIaBruta>(keys));
        }
        catch { /* índice é otimização; ignorar falha */ }
    }

    public async Task SalvarAsync(AnaliseIaBruta registro)
    {
        try
        {
            await _collection.InsertOneAsync(registro);
        }
        catch
        {
            // Mongo indisponível não pode quebrar a análise — apenas não cacheia.
        }
    }

    public async Task<AnaliseIaBruta?> BuscarPorHashAsync(string hashPdf)
    {
        if (string.IsNullOrWhiteSpace(hashPdf)) return null;
        try
        {
            return await _collection
                .Find(x => x.HashPdf == hashPdf && x.Sucesso)
                .SortByDescending(x => x.DataHora)
                .FirstOrDefaultAsync();
        }
        catch
        {
            return null;
        }
    }
}
