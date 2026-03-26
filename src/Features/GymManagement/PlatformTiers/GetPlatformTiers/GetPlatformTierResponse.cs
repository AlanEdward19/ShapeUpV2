namespace ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record GetPlatformTierResponse(int Id, string Name, string? Description, PlatformRoleType TargetRole, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);
