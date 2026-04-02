namespace ShapeUp.Features.GymManagement.TrainerClients.UnassignTrainerClient;

public record UnassignTrainerClientResponse(int TrainerId, int ClientId, DateTime UnassignedAtUtc);

