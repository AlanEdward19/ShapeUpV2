namespace ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record UpdatePlatformTierResponse(int Id, string Name, string? Description, PlatformRoleType TargetRole, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);

