namespace ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;

public record UpdatePlatformTierCommand(int Id, string Name, string? Description, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);

