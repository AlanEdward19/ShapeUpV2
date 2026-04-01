namespace ShapeUp.Features.Training.Infrastructure.Mongo;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Abstractions;
using Shared.Documents;

public class MongoWorkoutPlanRepository : IWorkoutPlanRepository
{
    private readonly IMongoCollection<WorkoutPlanDocument> _collection;

    public MongoWorkoutPlanRepository(IMongoClient mongoClient, IOptions<TrainingMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<WorkoutPlanDocument>(opts.WorkoutPlansCollectionName);

        var indexKeys = Builders<WorkoutPlanDocument>.IndexKeys
            .Descending(x => x.TargetUserId)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<WorkoutPlanDocument>(indexKeys));
    }

    public async Task AddAsync(WorkoutPlanDocument plan, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(plan, cancellationToken: cancellationToken);

    public async Task<WorkoutPlanDocument?> GetByIdAsync(string planId, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == planId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkoutPlanDocument>> GetByTargetUserKeysetAsync(
        int targetUserId,
        DateTime? createdBeforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutPlanDocument>.Filter.Eq(x => x.TargetUserId, targetUserId);
        if (createdBeforeUtc.HasValue)
            filter &= Builders<WorkoutPlanDocument>.Filter.Lt(x => x.CreatedAtUtc, createdBeforeUtc.Value);

        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(WorkoutPlanDocument plan, CancellationToken cancellationToken) =>
        await _collection.ReplaceOneAsync(x => x.Id == plan.Id, plan, cancellationToken: cancellationToken);

    public async Task DeleteAsync(string planId, CancellationToken cancellationToken) =>
        await _collection.DeleteOneAsync(x => x.Id == planId, cancellationToken: cancellationToken);
}
