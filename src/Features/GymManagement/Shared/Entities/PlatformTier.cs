namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class PlatformTier
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public PlatformRoleType TargetRole { get; set; }
    public decimal Price { get; set; }
    public int? MaxClients { get; set; }
    public int? MaxTrainers { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

