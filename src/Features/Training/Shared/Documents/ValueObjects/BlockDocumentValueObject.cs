using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class BlockDocumentValueObject
{
    [BsonRepresentation(BsonType.String)]
    public BlockType Type { get; set; } = BlockType.Straight;
    public List<BlockExerciseDocumentValueObject> Exercises { get; set; } = [];
    public int? TimeCapSeconds { get; set; }
    public int? IntervalSeconds { get; set; }
    public int? TotalRounds { get; set; }
    public int? RestAfterSeconds { get; set; }
}
