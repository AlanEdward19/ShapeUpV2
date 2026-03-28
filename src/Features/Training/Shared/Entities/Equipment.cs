namespace ShapeUp.Features.Training.Shared.Entities;

public class Equipment
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string NamePt { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ExerciseEquipment> ExerciseEquipments { get; set; } = [];
}
