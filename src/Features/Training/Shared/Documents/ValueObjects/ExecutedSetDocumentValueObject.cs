using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class ExecutedSetDocumentValueObject
{
    public int Repetitions { get; set; }
    public decimal Load { get; set; }
    [BsonRepresentation(BsonType.String)]
    public LoadUnit LoadUnit { get; set; } = LoadUnit.Kg;
    [BsonRepresentation(BsonType.String)]
    public SetType SetType { get; set; } = SetType.Working;
    [BsonRepresentation(BsonType.String)]
    public Technique Technique { get; set; } = Technique.Straight;
    public int Rpe { get; set; }
    public int RestSeconds { get; set; }
    public bool IsExtra { get; set; }

    public decimal Volume => Load * Repetitions;
}