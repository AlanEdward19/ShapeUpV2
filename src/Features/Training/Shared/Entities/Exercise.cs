namespace ShapeUp.Features.Training.Shared.Entities;

public class Exercise
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NamePt { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExerciseMuscleProfile> MuscleProfiles { get; set; } = [];
    public ICollection<ExerciseEquipment> ExerciseEquipments { get; set; } = [];
    public ICollection<ExerciseStep> Steps { get; set; } = [];
}

public class ExerciseStep
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public required string Description { get; set; }

    public Exercise? Exercise { get; set; }
}
