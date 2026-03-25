namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Represents a group that users can join and receive scopes from.
/// </summary>
public class Group
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserGroup> Members { get; set; } = [];
    public ICollection<GroupScope> Scopes { get; set; } = [];
}

