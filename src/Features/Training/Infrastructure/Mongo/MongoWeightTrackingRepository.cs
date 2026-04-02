using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;

namespace ShapeUp.Features.Training.Infrastructure.Mongo;

public class MongoWeightTrackingRepository : IWeightTrackingRepository
{
    private readonly IMongoCollection<WeightTargetDocument> _targetsCollection;
    private readonly IMongoCollection<WeightRegisterDocument> _registersCollection;

    public MongoWeightTrackingRepository(IMongoClient mongoClient, IOptions<TrainingMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);

        _targetsCollection = database.GetCollection<WeightTargetDocument>(opts.WeightTargetsCollectionName);
        _registersCollection = database.GetCollection<WeightRegisterDocument>(opts.WeightRegistersCollectionName);

        var targetIndex = Builders<WeightTargetDocument>.IndexKeys.Ascending(x => x.UserId);
        _targetsCollection.Indexes.CreateOne(new CreateIndexModel<WeightTargetDocument>(targetIndex, new CreateIndexOptions { Unique = true }));

        var registerIndex = Builders<WeightRegisterDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.Day);
        _registersCollection.Indexes.CreateOne(new CreateIndexModel<WeightRegisterDocument>(registerIndex, new CreateIndexOptions { Unique = true }));
    }

    public async Task UpsertTargetWeightAsync(int userId, decimal targetWeight, DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        var filter = Builders<WeightTargetDocument>.Filter.Eq(x => x.UserId, userId);
        var update = Builders<WeightTargetDocument>.Update
            .Set(x => x.TargetWeight, targetWeight)
            .Set(x => x.UpdatedAtUtc, updatedAtUtc);

        await _targetsCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<WeightTargetDocument?> GetTargetByUserIdAsync(int userId, CancellationToken cancellationToken) =>
        await _targetsCollection.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);

    public async Task UpsertDailyWeightAsync(int userId, decimal weight, DateOnly day, DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        var dayText = day.ToString("yyyy-MM-dd");
        var filter = Builders<WeightRegisterDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<WeightRegisterDocument>.Filter.Eq(x => x.Day, dayText);

        var update = Builders<WeightRegisterDocument>.Update
            .SetOnInsert(x => x.CreatedAtUtc, updatedAtUtc)
            .Set(x => x.Weight, weight)
            .Set(x => x.Day, dayText)
            .Set(x => x.UpdatedAtUtc, updatedAtUtc);

        await _registersCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<IReadOnlyList<WeightRegisterDocument>> GetRegistersByRangeAsync(
        int userId,
        DateOnly startDate,
        DateOnly endDateInclusive,
        CancellationToken cancellationToken)
    {
        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDateInclusive.ToString("yyyy-MM-dd");

        var filter = Builders<WeightRegisterDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<WeightRegisterDocument>.Filter.Gte(x => x.Day, start)
                     & Builders<WeightRegisterDocument>.Filter.Lte(x => x.Day, end);

        return await _registersCollection
            .Find(filter)
            .SortBy(x => x.Day)
            .ToListAsync(cancellationToken);
    }
}
