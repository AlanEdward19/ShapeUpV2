using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShapeUp.Features.Training.Shared.Documents;

public class WeightTargetDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int UserId { get; set; }
    public decimal TargetWeight { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
