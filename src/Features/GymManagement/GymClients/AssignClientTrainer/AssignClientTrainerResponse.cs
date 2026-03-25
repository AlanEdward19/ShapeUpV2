namespace ShapeUp.Features.GymManagement.GymClients.AssignClientTrainer;

public record AssignClientTrainerResponse(int ClientId, int GymId, int? TrainerId);