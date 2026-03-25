namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Represents a direct scope assignment to a user (outside of groups).
/// </summary>
public class UserScope
{
    public int UserId { get; set; }

    public int ScopeId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Scope? Scope { get; set; }
}

