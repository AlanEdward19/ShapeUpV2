using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoHydrationRepository : IHydrationRepository
{
    private readonly IMongoCollection<HydrationDayDocument> _collection;

    public MongoHydrationRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        _collection = mongoClient.GetDatabase(opts.DatabaseName).GetCollection<HydrationDayDocument>(opts.HydrationDaysCollectionName);

        var index = Builders<HydrationDayDocument>.IndexKeys
            .Ascending(x => x.UserId)
            .Ascending(x => x.Day);
        _collection.Indexes.CreateOne(new CreateIndexModel<HydrationDayDocument>(index, new CreateIndexOptions { Unique = true }));
    }

    public async Task<HydrationDayDocument?> GetDayAsync(int userId, DateOnly day, CancellationToken cancellationToken)
    {
        var dayText = day.ToString("yyyy-MM-dd");
        return await _collection.Find(x => x.UserId == userId && x.Day == dayText).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HydrationDayDocument>> GetRangeAsync(int userId, DateOnly startDate, DateOnly endDateInclusive, CancellationToken cancellationToken)
    {
        var start = startDate.ToString("yyyy-MM-dd");
        var end = endDateInclusive.ToString("yyyy-MM-dd");

        var filter = Builders<HydrationDayDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<HydrationDayDocument>.Filter.Gte(x => x.Day, start)
                     & Builders<HydrationDayDocument>.Filter.Lte(x => x.Day, end);

        return await _collection.Find(filter).SortBy(x => x.Day).ToListAsync(cancellationToken);
    }

    public async Task UpsertDayAsync(int userId, DateOnly day, int totalMl, DateTime updatedAtUtc, CancellationToken cancellationToken)
    {
        var dayText = day.ToString("yyyy-MM-dd");
        var filter = Builders<HydrationDayDocument>.Filter.Eq(x => x.UserId, userId)
                     & Builders<HydrationDayDocument>.Filter.Eq(x => x.Day, dayText);

        var update = Builders<HydrationDayDocument>.Update
            .SetOnInsert(x => x.CreatedAtUtc, updatedAtUtc)
            .Set(x => x.TotalMl, totalMl)
            .Set(x => x.UpdatedAtUtc, updatedAtUtc);

        try
        {
            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Two first writes of the day raced on the unique (user, day) index: the other one created it, so just update.
            await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
    }
}
