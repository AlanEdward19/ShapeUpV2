namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Represents a scope/permission in the system.
/// Format: Domain:Subdomain:Action (e.g., "groups:management:create", "users:profile:read")
/// </summary>
public class Scope
{
    public int Id { get; set; }

    /// <summary>
    /// Full scope identifier in format: domain:subdomain:action
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The domain part of the scope (e.g., "groups", "users", "orders")
    /// </summary>
    public required string Domain { get; set; }

    /// <summary>
    /// The subdomain part of the scope (e.g., "management", "profile", "fulfillment")
    /// </summary>
    public required string Subdomain { get; set; }

    /// <summary>
    /// The action part of the scope (e.g., "create", "read", "update", "delete")
    /// </summary>
    public required string Action { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserScope> Users { get; set; } = [];
    public ICollection<GroupScope> Groups { get; set; } = [];
}

