namespace ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;

public record AddTrainerClientResponse(int Id, int TrainerId, int ClientId, int TrainerPlanId, DateTime EnrolledAt);