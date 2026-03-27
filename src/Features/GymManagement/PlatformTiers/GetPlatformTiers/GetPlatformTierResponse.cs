namespace ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;

using Shared.Entities;

public record GetPlatformTierResponse(int Id, string Name, string? Description, PlatformRoleType TargetRole, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);
