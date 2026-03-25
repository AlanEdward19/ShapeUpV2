namespace ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;

public record CreatePlatformTierCommand(
    string Name,
    string? Description,
    decimal Price,
    int? MaxClients,
    int? MaxTrainers);

