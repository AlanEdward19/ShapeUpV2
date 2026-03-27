namespace ShapeUp.Features.Training.Shared.Entities;

public class ExerciseMuscleProfile
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public int MuscleId { get; set; }
    public decimal ActivationPercent { get; set; }

    public Exercise? Exercise { get; set; }
    public Muscle? Muscle { get; set; }
}
