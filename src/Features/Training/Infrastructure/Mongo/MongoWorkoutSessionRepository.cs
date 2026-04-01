using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Infrastructure.Mongo;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shared.Abstractions;
using Shared.Documents;

public class MongoWorkoutSessionRepository : IWorkoutSessionRepository
{
    private readonly IMongoCollection<WorkoutSessionDocument> _collection;

    public MongoWorkoutSessionRepository(IMongoClient mongoClient, IOptions<TrainingMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<WorkoutSessionDocument>(opts.WorkoutSessionsCollectionName);

        var indexKeys = Builders<WorkoutSessionDocument>.IndexKeys
            .Descending(x => x.TargetUserId)
            .Descending(x => x.StartedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<WorkoutSessionDocument>(indexKeys));
    }

    public async Task AddAsync(WorkoutSessionDocument session, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(session, cancellationToken: cancellationToken);

    public async Task<WorkoutSessionDocument?> GetByIdAsync(string sessionId, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == sessionId).FirstOrDefaultAsync(cancellationToken);

    public async Task<WorkoutSessionDocument?> GetActiveByTargetUserIdAsync(int targetUserId, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.TargetUserId, targetUserId)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCompleted, false)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCancelled, false);

        return await _collection.Find(filter)
            .SortByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateStateAsync(
        string sessionId,
        DateTime savedAtUtc,
        List<ExecutedExerciseDocumentValueObject> exercises,
        CancellationToken cancellationToken)
    {
        var update = Builders<WorkoutSessionDocument>.Update
            .Set(x => x.Exercises, exercises)
            .Set(x => x.LastSavedAtUtc, savedAtUtc);

        await _collection.UpdateOneAsync(x => x.Id == sessionId, update, cancellationToken: cancellationToken);
    }

    public async Task UpdateCompletionAsync(
        string sessionId,
        DateTime endedAtUtc,
        int perceivedExertion,
        List<WorkoutPrDocumentValueObject> personalRecords,
        CancellationToken cancellationToken)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
            return;

        var durationSeconds = (int)Math.Max(0, (endedAtUtc - session.StartedAtUtc).TotalSeconds);

        var update = Builders<WorkoutSessionDocument>.Update
            .Set(x => x.EndedAtUtc, endedAtUtc)
            .Set(x => x.LastSavedAtUtc, endedAtUtc)
            .Set(x => x.IsCompleted, true)
            .Set(x => x.PerceivedExertion, perceivedExertion)
            .Set(x => x.DurationSeconds, durationSeconds)
            .Set(x => x.PersonalRecords, personalRecords);

        await _collection.UpdateOneAsync(x => x.Id == sessionId, update, cancellationToken: cancellationToken);
    }

    public async Task CancelAsync(string sessionId, DateTime cancelledAtUtc, int durationSeconds, CancellationToken cancellationToken)
    {
        var update = Builders<WorkoutSessionDocument>.Update
            .Set(x => x.EndedAtUtc, cancelledAtUtc)
            .Set(x => x.CancelledAtUtc, cancelledAtUtc)
            .Set(x => x.LastSavedAtUtc, cancelledAtUtc)
            .Set(x => x.DurationSeconds, durationSeconds)
            .Set(x => x.IsCancelled, true);

        await _collection.UpdateOneAsync(x => x.Id == sessionId, update, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<WorkoutSessionDocument>> GetByTargetUserKeysetAsync(
        int targetUserId,
        DateTime? startedBeforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.TargetUserId, targetUserId);
        if (startedBeforeUtc.HasValue)
            filter &= Builders<WorkoutSessionDocument>.Filter.Lt(x => x.StartedAtUtc, startedBeforeUtc.Value);

        return await _collection.Find(filter)
            .SortByDescending(x => x.StartedAtUtc)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkoutSessionDocument>> GetCompletedByUserInRangeAsync(
        int targetUserId,
        DateTime startInclusiveUtc,
        DateTime endExclusiveUtc,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.TargetUserId, targetUserId)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCompleted, true)
                     & Builders<WorkoutSessionDocument>.Filter.Gte(x => x.StartedAtUtc, startInclusiveUtc)
                     & Builders<WorkoutSessionDocument>.Filter.Lt(x => x.StartedAtUtc, endExclusiveUtc);

        return await _collection.Find(filter)
            .SortByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }
}

