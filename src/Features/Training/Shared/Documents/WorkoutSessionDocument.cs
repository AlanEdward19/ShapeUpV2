using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace ShapeUp.Features.Training.Shared.Documents;

public class WorkoutSessionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public string? WorkoutPlanId { get; set; }
    public int TargetUserId { get; set; }
    public int ExecutedByUserId { get; set; }
    public int? TrainerUserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime? LastSavedAtUtc { get; set; }
    public int? PerceivedExertion { get; set; }
    public int? DurationSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCancelled { get; set; }

    public List<ExecutedExerciseDocumentValueObject> Exercises { get; set; } = [];
    public List<WorkoutPrDocumentValueObject> PersonalRecords { get; set; } = [];
}