namespace ShapeUp.Features.Training.Shared.Entities;

public class ExerciseEquivalent
{
    public int ExerciseId { get; set; }
    public int EquivalentExerciseId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Exercise? Exercise { get; set; }
    public Exercise? EquivalentExercise { get; set; }
}
