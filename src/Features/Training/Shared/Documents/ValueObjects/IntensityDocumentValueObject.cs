using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class IntensityDocumentValueObject
{
    [BsonRepresentation(BsonType.String)]
    public IntensityType Type { get; set; }
    public int Value { get; set; }
}
