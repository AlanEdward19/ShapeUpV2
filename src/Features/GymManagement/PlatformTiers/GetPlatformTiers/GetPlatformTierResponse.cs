namespace ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;

public record GetPlatformTierResponse(int Id, string Name, string? Description, decimal Price, int? MaxClients, int? MaxTrainers, bool IsActive);