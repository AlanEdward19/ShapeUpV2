namespace ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record CreatePlatformTierCommand(
    string Name,
    string? Description,
    PlatformRoleType TargetRole,
    decimal Price,
    int? MaxClients,
    int? MaxTrainers);

