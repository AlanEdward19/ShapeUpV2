namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Represents a scope assignment to a group.
/// All members of the group inherit these scopes.
/// </summary>
public class GroupScope
{
    public int GroupId { get; set; }

    public int ScopeId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Group? Group { get; set; }
    public Scope? Scope { get; set; }
}

