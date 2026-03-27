namespace ShapeUp.Features.Training.Shared.Entities;

using Enums;

public class ExerciseMuscleProfile
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public MuscleGroup MuscleGroup { get; set; }
    public decimal ActivationPercent { get; set; }

    public Exercise? Exercise { get; set; }
}
