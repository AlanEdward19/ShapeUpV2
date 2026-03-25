namespace ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;

public record CreateTrainerPlanResponse(int Id, int TrainerId, string Name, string? Description, decimal Price, int DurationDays);