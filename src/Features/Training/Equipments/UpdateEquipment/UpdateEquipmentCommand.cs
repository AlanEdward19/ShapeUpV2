namespace ShapeUp.Features.Training.Equipments.UpdateEquipment;

public record UpdateEquipmentCommand(int EquipmentId, string Name, string NamePt, string? Description);