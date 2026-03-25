namespace ShapeUp.Features.GymManagement.TrainerClients.TransferTrainerClient;

public record TransferTrainerClientCommand(int ClientId, int NewTrainerId, int NewPlanId);