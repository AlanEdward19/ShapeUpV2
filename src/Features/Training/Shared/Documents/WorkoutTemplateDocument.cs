using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Shared.Documents;

public class WorkoutTemplateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int DurationInWeeks { get; set; }
    public string Phase { get; set; } = string.Empty;
    [BsonRepresentation(BsonType.String)]
    public Difficulty Difficulty { get; set; } = Difficulty.Intermediate;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<PlannedExerciseDocumentValueObject> Exercises { get; set; } = [];
}

