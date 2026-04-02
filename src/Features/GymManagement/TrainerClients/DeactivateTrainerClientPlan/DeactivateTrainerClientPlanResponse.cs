namespace ShapeUp.Features.GymManagement.TrainerClients.DeactivateTrainerClientPlan;

public record DeactivateTrainerClientPlanResponse(int TrainerId, int ClientId, bool IsActive, DateTime UpdatedAtUtc);


