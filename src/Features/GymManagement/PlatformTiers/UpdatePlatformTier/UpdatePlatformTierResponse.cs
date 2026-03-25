namespace ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;

public record UpdatePlatformTierResponse(int Id, string Name, string? Description, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);

