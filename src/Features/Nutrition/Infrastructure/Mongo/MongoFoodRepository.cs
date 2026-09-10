using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Shared.Pagination;

namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class MongoFoodRepository : IFoodRepository
{
    private readonly IMongoCollection<FoodDocument> _collection;

    public MongoFoodRepository(IMongoClient mongoClient, IOptions<NutritionMongoOptions> options)
    {
        var opts = options.Value;
        var database = mongoClient.GetDatabase(opts.DatabaseName);
        _collection = database.GetCollection<FoodDocument>(opts.FoodsCollectionName);

        var barcodeIndex = Builders<FoodDocument>.IndexKeys.Ascending(x => x.Barcode);
        _collection.Indexes.CreateOne(new CreateIndexModel<FoodDocument>(
            barcodeIndex,
            new CreateIndexOptions
            {
                Unique = true,
                Sparse = true,
                Name = "IX_Foods_Barcode_Unique_Sparse"
            }));

        var nameIndex = Builders<FoodDocument>.IndexKeys
            .Ascending(x => x.Name)
            .Descending(x => x.CreatedAtUtc);
        _collection.Indexes.CreateOne(new CreateIndexModel<FoodDocument>(nameIndex));
    }

    public async Task CreateAsync(FoodDocument food, CancellationToken cancellationToken) =>
        await _collection.InsertOneAsync(food, cancellationToken: cancellationToken);

    public async Task<FoodDocument?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var filter = Builders<FoodDocument>.Filter.Eq(x => x.Id, id)
                     & Builders<FoodDocument>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FoodDocument?> GetByIdIncludingDeletedAsync(string id, CancellationToken cancellationToken) =>
        await _collection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<FoodDocument?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var filter = Builders<FoodDocument>.Filter.Eq(x => x.Barcode, barcode)
                     & Builders<FoodDocument>.Filter.Eq(x => x.IsDeleted, false);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<FoodDocument> Items, string? NextCursor)> SearchAsync(
        string query,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoodDocument>.Filter.Eq(x => x.IsDeleted, false);

        if (!string.IsNullOrWhiteSpace(query))
        {
            filter &= Builders<FoodDocument>.Filter.Regex(
                x => x.Name,
                new BsonRegularExpression(query, "i"));
        }

        if (KeysetCursorCodec.TryDecodeLong(cursor, out var cursorBinary))
        {
            var createdBeforeUtc = DateTime.FromBinary(cursorBinary);
            filter &= Builders<FoodDocument>.Filter.Lt(x => x.CreatedAtUtc, createdBeforeUtc);
        }

        var items = await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        var nextCursor = items.Count < pageSize
            ? null
            : KeysetCursorCodec.EncodeLong(items[^1].CreatedAtUtc.ToBinary());

        return (items, nextCursor);
    }

    public async Task ApplyApprovedOverrideAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken)
    {
        var update = Builders<FoodDocument>.Update
            .Set(x => x.MacrosPer100, overrideDocument.MacrosPer100)
            .Set(x => x.MicrosPer100, overrideDocument.MicrosPer100);

        await _collection.UpdateOneAsync(
            x => x.Id == overrideDocument.FoodId && !x.IsDeleted,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(
        string id,
        int deletedByUserId,
        DateTime deletedAtUtc,
        CancellationToken cancellationToken)
    {
        var update = Builders<FoodDocument>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedByUserId, deletedByUserId)
            .Set(x => x.DeletedAtUtc, deletedAtUtc);

        var result = await _collection.UpdateOneAsync(
            x => x.Id == id && !x.IsDeleted,
            update,
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }
}
