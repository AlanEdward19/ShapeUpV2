namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class TrainerClient
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public int ClientId { get; set; }
    public int TrainerPlanId { get; set; }
    public TrainerPlan? TrainerPlan { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

