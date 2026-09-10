using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Shared.Pagination;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoFoodModerationRepository : IFoodModerationRepository
{
    private readonly IMongoCollection<FoodModerationRequestDocument> _collection;

    public MongoFoodModerationRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<FoodModerationRequestDocument>(opts.FoodModerationRequestsCollectionName);

        var index = Builders<FoodModerationRequestDocument>.IndexKeys
            .Ascending(x => x.Status)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<FoodModerationRequestDocument>(index));
    }

    public async Task CreateAsync(FoodModerationRequestDocument request, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(request, cancellationToken: cancellationToken);

    public async Task<IReadOnlyList<FoodModerationRequestDocument>> GetPendingAsync(
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoodModerationRequestDocument>.Filter.Eq(x => x.Status, "Pending");

        if (KeysetCursorCodec.TryDecodeLong(cursor, out var cursorBinary))
        {
            var createdBeforeUtc = DateTime.FromBinary(cursorBinary);
            filter &= Builders<FoodModerationRequestDocument>.Filter.Lt(x => x.CreatedAtUtc, createdBeforeUtc);
        }

        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DecideAsync(
        string requestId,
        string decision,
        int decidedByUserId,
        DateTime decidedAtUtc,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoodModerationRequestDocument>.Filter.Eq(x => x.Id, requestId)
                     & Builders<FoodModerationRequestDocument>.Filter.Eq(x => x.Status, "Pending");

        var update = Builders<FoodModerationRequestDocument>.Update
            .Set(x => x.Status, decision)
            .Set(x => x.DecidedByUserId, decidedByUserId)
            .Set(x => x.DecidedAtUtc, decidedAtUtc);

        var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
