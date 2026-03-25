namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class GymStaff
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public Gym? Gym { get; set; }
    public int UserId { get; set; }
    public GymStaffRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime HiredAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GymClient> AssignedClients { get; set; } = [];
}

