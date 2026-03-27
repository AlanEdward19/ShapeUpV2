namespace ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;

using Shared.Entities;

public record UpdatePlatformTierCommand(int Id, string Name, string? Description, PlatformRoleType TargetRole, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);

