namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class TrainerClientInvite
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string AccessTokenHash { get; set; } = string.Empty;
    public int? TrainerPlanId { get; set; }
    public TrainerPlan? TrainerPlan { get; set; }
    public TrainerClientInviteStatus Status { get; set; } = TrainerClientInviteStatus.Invited;
    public DateTime ExpiresAtUtc { get; set; }
    public int? AcceptedByUserId { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

