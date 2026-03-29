namespace ShapeUp.Features.Training.Infrastructure.Mongo;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;

public class MongoWorkoutTemplateRepository : IWorkoutTemplateRepository
{
    private readonly IMongoCollection<WorkoutTemplateDocument> _collection;

    public MongoWorkoutTemplateRepository(IMongoClient mongoClient, IOptions<TrainingMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<WorkoutTemplateDocument>(opts.WorkoutTemplatesCollectionName);

        var indexKeys = Builders<WorkoutTemplateDocument>.IndexKeys
            .Descending(x => x.CreatedByUserId)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<WorkoutTemplateDocument>(indexKeys));
    }

    public async Task AddAsync(WorkoutTemplateDocument template, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(template, cancellationToken: cancellationToken);

    public async Task<WorkoutTemplateDocument?> GetByIdAsync(string templateId, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == templateId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkoutTemplateDocument>> GetByCreatorKeysetAsync(
        int creatorUserId,
        DateTime? createdBeforeUtc,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WorkoutTemplateDocument>.Filter.Eq(x => x.CreatedByUserId, creatorUserId);
        if (createdBeforeUtc.HasValue)
            filter &= Builders<WorkoutTemplateDocument>.Filter.Lt(x => x.CreatedAtUtc, createdBeforeUtc.Value);

        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }
}

