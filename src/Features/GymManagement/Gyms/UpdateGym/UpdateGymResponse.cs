namespace ShapeUp.Features.GymManagement.Gyms.UpdateGym;

public record UpdateGymResponse(
    int Id,
    int OwnerId,
    string Name,
    string? Description,
    string? Address,
    int? PlatformTierId,
    bool IsActive,
    DateTime? UpdatedAt);

