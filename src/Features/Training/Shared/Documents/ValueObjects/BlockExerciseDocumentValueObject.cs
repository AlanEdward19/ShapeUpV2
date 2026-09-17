namespace ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class BlockExerciseDocumentValueObject
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public double? StrengthGainPercentage { get; set; }
    public bool RequireRpe { get; set; } = false;
    public List<PlannedSetDocumentValueObject> Sets { get; set; } = [];
}

