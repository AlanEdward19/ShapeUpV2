namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class ExecutedExerciseDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public bool RequireRpe { get; set; } = false;
    public List<ExecutedSetDocumentValueObject> Sets { get; set; } = [];
}