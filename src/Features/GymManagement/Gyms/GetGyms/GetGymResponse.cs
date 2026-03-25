namespace ShapeUp.Features.GymManagement.Gyms.GetGyms;

public record GetGymResponse(int Id, int OwnerId, string Name, string? Description, string? Address, string? PlatformTierName, DateTime CreatedAt);