namespace ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;

public record CreatePlatformTierResponse(int Id, string Name, string? Description, decimal Price, int? MaxClients, int? MaxTrainers);

