using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared.Documents;

public class FoodDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Barcode { get; set; }
    public MacroValueObject MacrosPer100 { get; set; } = null!;
    public MicroValueObject? MicrosPer100 { get; set; }
    public HouseholdMeasure? Measure { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public int? DeletedByUserId { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
