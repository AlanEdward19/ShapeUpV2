namespace ShapeUp.Features.GymManagement.GymClients.AssignClientTrainer;

public record AssignClientTrainerCommand(int GymId, int ClientId, int? TrainerId);