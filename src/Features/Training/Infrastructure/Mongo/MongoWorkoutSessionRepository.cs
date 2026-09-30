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

    public async Task<WorkoutSessionDocument?> GetLatestCompletedByWorkoutPlanIdAsync(string workoutPlanId, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.WorkoutPlanId, workoutPlanId)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCompleted, true)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCancelled, false);

        return await _collection.Find(filter)
            .SortByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkoutSessionDocument?> GetActiveByTargetUserIdAsync(int targetUserId, CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.TargetUserId, targetUserId)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCompleted, false)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.IsCancelled, false)
                     // Active session must not be finished by timestamp markers.
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.EndedAtUtc, null)
                     & Builders<WorkoutSessionDocument>.Filter.Eq(x => x.CancelledAtUtc, null);

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

    public async Task<bool> AppendSetAsync(
        string sessionId,
        string operationId,
        ExecutedExerciseDocumentValueObject exerciseIfMissing,
        ExecutedSetDocumentValueObject set,
        DateTime savedAtUtc,
        CancellationToken cancellationToken)
    {
        var f = Builders<WorkoutSessionDocument>.Filter;
        var u = Builders<WorkoutSessionDocument>.Update;
        var exerciseId = exerciseIfMissing.ExerciseId;

        // Guard on the operation id (and open session) inside the filter so concurrent retries apply exactly once.
        var open = f.Eq(x => x.Id, sessionId) & f.Eq(x => x.IsCompleted, false) & f.Eq(x => x.IsCancelled, false);
        var notApplied = open & f.Not(f.AnyEq(x => x.AppliedSetOperationIds, operationId));

        // Two concurrent operations may both miss an exercise that is not in the session yet; the loser
        // of the push retries and then finds the exercise created by the winner.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var existingExercise = await _collection.UpdateOneAsync(
                notApplied & f.ElemMatch(x => x.Exercises, e => e.ExerciseId == exerciseId),
                u.Push("Exercises.$.Sets", set)
                    .AddToSet(x => x.AppliedSetOperationIds, operationId)
                    .Set(x => x.LastSavedAtUtc, savedAtUtc),
                cancellationToken: cancellationToken);
            if (existingExercise.ModifiedCount > 0)
                return true;

            exerciseIfMissing.Sets = [set];
            var newExercise = await _collection.UpdateOneAsync(
                notApplied & f.Not(f.ElemMatch(x => x.Exercises, e => e.ExerciseId == exerciseId)),
                u.Push(x => x.Exercises, exerciseIfMissing)
                    .AddToSet(x => x.AppliedSetOperationIds, operationId)
                    .Set(x => x.LastSavedAtUtc, savedAtUtc),
                cancellationToken: cancellationToken);
            if (newExercise.ModifiedCount > 0)
                return true;

            // Nothing to retry when the operation was already applied or the session is gone/closed.
            if (await _collection.CountDocumentsAsync(notApplied, cancellationToken: cancellationToken) == 0)
                return false;
        }

        return false;
    }

    public async Task UpdateCompletionAsync(
        string sessionId,
        DateTime endedAtUtc,
        int perceivedExertion,
        List<WorkoutPrDocumentValueObject> personalRecords,
        CancellationToken cancellationToken,
        IClientSessionHandle? mongoSession = null)
    {
        var session = mongoSession is null
            ? await GetByIdAsync(sessionId, cancellationToken)
            : await _collection.Find(mongoSession, x => x.Id == sessionId).FirstOrDefaultAsync(cancellationToken);

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

        var filter = Builders<WorkoutSessionDocument>.Filter.Eq(x => x.Id, sessionId);

        if (mongoSession is null)
            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        else
            await _collection.UpdateOneAsync(mongoSession, filter, update, cancellationToken: cancellationToken);
    }

    public async Task CancelAsync(string sessionId, CancellationToken cancellationToken) =>
        await _collection.DeleteOneAsync(x => x.Id == sessionId, cancellationToken);

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

