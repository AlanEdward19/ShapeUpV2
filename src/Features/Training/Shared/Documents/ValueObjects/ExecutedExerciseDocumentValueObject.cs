namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class ExecutedExerciseDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public List<ExecutedSetDocumentValueObject> Sets { get; set; } = [];
}