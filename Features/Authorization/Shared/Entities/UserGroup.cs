namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Enum representing roles a user can have within a group.
/// Owner is the only role that can delete the group.
/// </summary>
public enum GroupRole
{
    /// <summary>
    /// Group owner - can manage group, delete it, and manage members.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Group administrator - can manage members and group settings (except deletion).
    /// </summary>
    Administrator = 1,

    /// <summary>
    /// Group member - can only access group's scopes.
    /// </summary>
    Member = 2
}

/// <summary>
/// Represents the relationship between a User and a Group, including the user's role.
/// </summary>
public class UserGroup
{
    public int UserId { get; set; }

    public int GroupId { get; set; }

    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Group? Group { get; set; }
}

