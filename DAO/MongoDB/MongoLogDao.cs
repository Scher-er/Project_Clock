using BalancoPatrimonial.App.DAO.Interfaces;
using BalancoPatrimonial.App.Models.Enums;
using BalancoPatrimonial.App.Models.Log;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace BalancoPatrimonial.App.DAO.MongoDB;

/// <summary>
/// DAO dos logs do sistema. Usa <c>string</c> como ID (ObjectId do MongoDB serializado).
/// </summary>
public class MongoLogDao : ILogDao
{
    private readonly IMongoCollection<LogSistema> _collection;
    public string Fonte => "Mongo.logs";

    static MongoLogDao()
    {
        // Registra o mapping de BsonClassMap pra LogSistema uma única vez (na primeira inicialização).
        // Isso ensina o driver Mongo a:
        //   - Mapear a propriedade Id pro campo _id como ObjectId (mas exposto como string)
        //   - Indexar por DataHora (feito separadamente no método CriarIndicesAsync)
        if (!BsonClassMap.IsClassMapRegistered(typeof(LogSistema)))
        {
            BsonClassMap.RegisterClassMap<LogSistema>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(x => x.Id)
                  .SetIdGenerator(MongoDB.Bson.Serialization.IdGenerators.StringObjectIdGenerator.Instance)
                  .SetSerializer(new MongoDB.Bson.Serialization.Serializers.StringSerializer(BsonType.ObjectId));
            });
        }
    }

    public MongoLogDao(IMongoConnectionFactory factory, DatabaseSettings settings)
    {
        _collection = factory.ObterCollection<LogSistema>(settings.MongoCollectionLogs);
        // Garante que o índice por DataHora existe (fire-and-forget seguro)
        _ = CriarIndicesAsync();
    }

    private async Task CriarIndicesAsync()
    {
        try
        {
            var modelos = new[]
            {
                new CreateIndexModel<LogSistema>(
                    Builders<LogSistema>.IndexKeys.Descending(l => l.DataHora)),
                new CreateIndexModel<LogSistema>(
                    Builders<LogSistema>.IndexKeys.Ascending(l => l.Usuario)),
                new CreateIndexModel<LogSistema>(
                    Builders<LogSistema>.IndexKeys.Ascending(l => l.TipoEvento))
            };
            await _collection.Indexes.CreateManyAsync(modelos);
        }
        catch
        {
            // Se MongoDB estiver fora, ignora — vai falhar no primeiro insert mesmo
        }
    }

    public async Task<string> InserirAsync(LogSistema log)
    {
        await _collection.InsertOneAsync(log);
        return log.Id;
    }

    public async Task<bool> AtualizarAsync(LogSistema log)
    {
        var resultado = await _collection.ReplaceOneAsync(
            l => l.Id == log.Id, log);
        return resultado.ModifiedCount > 0;
    }

    public async Task<bool> ExcluirAsync(string id)
    {
        var resultado = await _collection.DeleteOneAsync(l => l.Id == id);
        return resultado.DeletedCount > 0;
    }

    public async Task<LogSistema?> BuscarPorIdAsync(string id)
    {
        return await _collection.Find(l => l.Id == id).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<LogSistema>> ListarTodosAsync()
    {
        return await _collection.Find(Builders<LogSistema>.Filter.Empty)
            .SortByDescending(l => l.DataHora)
            .Limit(500)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogSistema>> ListarPorDataAsync(DateTime dia)
    {
        var inicio = dia.Date;
        var fim = inicio.AddDays(1);
        return await _collection.Find(l => l.DataHora >= inicio && l.DataHora < fim)
            .SortBy(l => l.DataHora)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogSistema>> ListarPorIntervaloAsync(DateTime inicio, DateTime fim)
    {
        return await _collection.Find(l => l.DataHora >= inicio && l.DataHora <= fim)
            .SortBy(l => l.DataHora)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogSistema>> ListarPorTipoAsync(TipoEventoLog tipo, DateTime? desde = null)
    {
        var filtros = Builders<LogSistema>.Filter.Eq(l => l.TipoEvento, tipo);
        if (desde is not null)
        {
            filtros &= Builders<LogSistema>.Filter.Gte(l => l.DataHora, desde.Value);
        }
        return await _collection.Find(filtros)
            .SortByDescending(l => l.DataHora)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogSistema>> ListarPorUsuarioAsync(string usuario, DateTime? desde = null)
    {
        var filtros = Builders<LogSistema>.Filter.Eq(l => l.Usuario, usuario);
        if (desde is not null)
        {
            filtros &= Builders<LogSistema>.Filter.Gte(l => l.DataHora, desde.Value);
        }
        return await _collection.Find(filtros)
            .SortByDescending(l => l.DataHora)
            .ToListAsync();
    }
}
