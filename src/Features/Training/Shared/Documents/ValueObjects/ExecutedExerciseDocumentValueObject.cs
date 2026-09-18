using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class ExecutedExerciseDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public bool RequireRpe { get; set; } = false;
    [BsonRepresentation(BsonType.String)]
    public ExerciseType ExerciseType { get; set; } = ExerciseType.WeightBased;
    public List<ExecutedSetDocumentValueObject> Sets { get; set; } = [];
}