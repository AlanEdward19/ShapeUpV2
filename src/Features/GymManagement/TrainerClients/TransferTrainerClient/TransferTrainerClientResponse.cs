namespace ShapeUp.Features.GymManagement.TrainerClients.TransferTrainerClient;

public record TransferTrainerClientResponse(int ClientId, int OldTrainerId, int NewTrainerId, int NewPlanId);