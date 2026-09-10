using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared.Documents;

public class FoodOverrideDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string FoodId { get; set; } = null!;
    public int UserId { get; set; }
    public MacroValueObject MacrosPer100 { get; set; } = null!;
    public MicroValueObject? MicrosPer100 { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
