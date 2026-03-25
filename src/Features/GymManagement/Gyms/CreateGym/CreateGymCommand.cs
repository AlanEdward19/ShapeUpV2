namespace ShapeUp.Features.GymManagement.Gyms.CreateGym;

public record CreateGymCommand(string Name, string? Description, string? Address, int? PlatformTierId);

