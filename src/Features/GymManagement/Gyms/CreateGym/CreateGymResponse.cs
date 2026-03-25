namespace ShapeUp.Features.GymManagement.Gyms.CreateGym;

public record CreateGymResponse(int Id, int OwnerId, string Name, string? Description, string? Address, int? PlatformTierId, DateTime CreatedAt);

