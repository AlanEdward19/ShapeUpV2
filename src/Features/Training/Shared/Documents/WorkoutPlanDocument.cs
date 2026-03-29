using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Shared.Documents;

public class WorkoutPlanDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int TargetUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? TrainerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<PlannedExerciseDocumentValueObject> Exercises { get; set; } = [];
}

