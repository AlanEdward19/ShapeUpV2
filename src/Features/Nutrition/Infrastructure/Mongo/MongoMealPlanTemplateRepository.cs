using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoMealPlanTemplateRepository : IMealPlanTemplateRepository
{
    private readonly IMongoCollection<MealPlanTemplateDocument> _collection;

    public MongoMealPlanTemplateRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        _collection = mongoClient.GetDatabase(opts.DatabaseName)
            .GetCollection<MealPlanTemplateDocument>(opts.MealPlanTemplatesCollectionName);

        var creatorIndex = Builders<MealPlanTemplateDocument>.IndexKeys
            .Ascending(x => x.CreatedByUserId)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<MealPlanTemplateDocument>(creatorIndex));
    }

    public async Task CreateAsync(MealPlanTemplateDocument template, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(template, cancellationToken: cancellationToken);

    public async Task<MealPlanTemplateDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MealPlanTemplateDocument>> GetByCreatorAsync(int createdByUserId, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.CreatedByUserId == createdByUserId)
            .SortByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(MealPlanTemplateDocument template, CancellationToken cancellationToken) =>
        await _collection.ReplaceOneAsync(x => x.Id == template.Id, template, cancellationToken: cancellationToken);

    public async Task DeleteAsync(string id, CancellationToken cancellationToken) =>
        await _collection.DeleteOneAsync(x => x.Id == id, cancellationToken);
}
