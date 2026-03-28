namespace ShapeUp.Features.GymManagement.Gyms.UpdateGym;

public record UpdateGymCommand(
    int GymId,
    string Name,
    string? Description,
    string? Address,
    int? PlatformTierId,
    bool IsActive);

