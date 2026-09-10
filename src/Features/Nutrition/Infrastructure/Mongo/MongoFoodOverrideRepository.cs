using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoFoodOverrideRepository : IFoodOverrideRepository
{
    private readonly IMongoCollection<FoodOverrideDocument> _collection;

    public MongoFoodOverrideRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<FoodOverrideDocument>(opts.FoodOverridesCollectionName);

        var index = Builders<FoodOverrideDocument>.IndexKeys
            .Ascending(x => x.FoodId)
            .Ascending(x => x.UserId);
        _collection.Indexes.CreateOne(new CreateIndexModel<FoodOverrideDocument>(index, new CreateIndexOptions { Unique = true }));
    }

    public async Task<FoodOverrideDocument?> GetActiveForUserAsync(
        string foodId,
        int userId,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoodOverrideDocument>.Filter.Eq(x => x.FoodId, foodId)
                     & Builders<FoodOverrideDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<FoodOverrideDocument>.Filter.Eq(x => x.IsActive, true);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(overrideDocument, cancellationToken: cancellationToken);

    public async Task SetActiveAsync(string overrideId, int userId, bool isActive, CancellationToken cancellationToken)
    {
        var filter = Builders<FoodOverrideDocument>.Filter.Eq(x => x.Id, overrideId)
                     & Builders<FoodOverrideDocument>.Filter.Eq(x => x.UserId, userId);
        var update = Builders<FoodOverrideDocument>.Update.Set(x => x.IsActive, isActive);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
