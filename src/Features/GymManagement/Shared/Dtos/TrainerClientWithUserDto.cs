namespace ShapeUp.Features.GymManagement.Shared.Dtos;

using Entities;

public class TrainerClientWithUserDto
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = null!;
    public string ClientEmail { get; set; } = null!;
    public int? TrainerPlanId { get; set; }
    public string? PlanName { get; set; }
    public bool IsActive { get; set; }
    public DateTime EnrolledAt { get; set; }
}

