namespace ShapeUp.Features.Training.Shared.Entities;

public class ExerciseEquipment
{
    public int ExerciseId { get; set; }
    public int EquipmentId { get; set; }

    public Exercise? Exercise { get; set; }
    public Equipment? Equipment { get; set; }
}

