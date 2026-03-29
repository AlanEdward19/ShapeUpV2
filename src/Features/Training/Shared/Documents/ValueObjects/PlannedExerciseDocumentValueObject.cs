namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class PlannedExerciseDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public List<PlannedSetDocumentValueObject> Sets { get; set; } = [];
}

