using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoMealPlanRepository : IMealPlanRepository
{
    private readonly IMongoCollection<MealPlanDocument> _collection;

    public MongoMealPlanRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<MealPlanDocument>(opts.MealPlansCollectionName);

        var userIndex = Builders<MealPlanDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<MealPlanDocument>(userIndex));

        var activeIndex = Builders<MealPlanDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.IsActive);
        _collection.Indexes.CreateOne(new CreateIndexModel<MealPlanDocument>(
            activeIndex,
            new CreateIndexOptions<MealPlanDocument>
            {
                PartialFilterExpression = Builders<MealPlanDocument>.Filter.Eq(x => x.IsActive, true)
            }));
    }

    public async Task CreateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(mealPlan, cancellationToken: cancellationToken);

    public async Task<MealPlanDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MealPlanDocument>> GetByUserAsync(int userId, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<MealPlanDocument?> GetActiveAsync(int userId, CancellationToken cancellationToken)
    {
        var filter = Builders<MealPlanDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<MealPlanDocument>.Filter.Eq(x => x.IsActive, true);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken) =>
        await _collection.ReplaceOneAsync(x => x.Id == mealPlan.Id, mealPlan, cancellationToken: cancellationToken);
}
