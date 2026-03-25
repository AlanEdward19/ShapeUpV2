namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class UserPlatformRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public PlatformRoleType Role { get; set; }
    public int? PlatformTierId { get; set; }
    public PlatformTier? PlatformTier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

