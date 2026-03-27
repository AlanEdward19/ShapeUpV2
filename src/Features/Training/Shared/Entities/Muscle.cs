namespace ShapeUp.Features.Training.Shared.Entities;

public class Muscle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NamePt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExerciseMuscleProfile> ExerciseProfiles { get; set; } = [];
}

