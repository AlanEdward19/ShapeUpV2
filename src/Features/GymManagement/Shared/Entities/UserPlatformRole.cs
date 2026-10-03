namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class UserPlatformRole
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public PlatformRoleType Role { get; set; }
    public int? PlatformTierId { get; set; }
    public PlatformTier? PlatformTier { get; set; }
    /// <summary>
    /// Set only when the role was granted by a verified professional credential; such roles are
    /// withdrawn when that credential stops being valid. Null means assigned by hand (admin),
    /// and the credential flow never removes it.
    /// </summary>
    public int? GrantedByCredentialId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

