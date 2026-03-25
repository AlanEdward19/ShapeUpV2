namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;

public record GetTrainerClientResponse(int Id, int TrainerId, int ClientId, string PlanName, DateTime EnrolledAt);