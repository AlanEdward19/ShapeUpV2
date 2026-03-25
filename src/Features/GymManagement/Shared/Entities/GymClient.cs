namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class GymClient
{
    public int Id { get; set; }
    public int GymId { get; set; }
    public Gym? Gym { get; set; }
    public int UserId { get; set; }
    public int GymPlanId { get; set; }
    public GymPlan? GymPlan { get; set; }
    public int? TrainerId { get; set; }
    public GymStaff? Trainer { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

