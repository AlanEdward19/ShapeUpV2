namespace ShapeUp.Features.Authorization.Shared.Entities;

/// <summary>
/// Represents a user in the system, provisioned from Firebase authentication.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Firebase UID for authentication and claims synchronization.
    /// </summary>
    public required string FirebaseUid { get; init; }

    public required string Email { get; init; }

    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<UserGroup> Groups { get; set; } = [];
    public ICollection<UserScope> Scopes { get; set; } = [];
}

