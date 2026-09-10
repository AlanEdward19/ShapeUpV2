using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShapeUp.Features.Nutrition.Shared.Documents;

public class FoodModerationRequestDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string FoodId { get; set; } = null!;
    public string FoodOverrideId { get; set; } = null!;
    public int RequestedByUserId { get; set; }
    public string Status { get; set; } = "Pending";
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
