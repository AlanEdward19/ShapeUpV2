namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class Gym
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public int? PlatformTierId { get; set; }
    public PlatformTier? PlatformTier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<GymPlan> Plans { get; set; } = [];
    public ICollection<GymStaff> Staff { get; set; } = [];
    public ICollection<GymClient> Clients { get; set; } = [];
}

