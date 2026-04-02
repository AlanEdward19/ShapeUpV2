using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShapeUp.Features.Training.Shared.Documents;

public class WeightRegisterDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int UserId { get; set; }
    public string Day { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
