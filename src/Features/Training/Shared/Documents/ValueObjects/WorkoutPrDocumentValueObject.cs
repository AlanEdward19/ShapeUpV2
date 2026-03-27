namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class WorkoutPrDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
}