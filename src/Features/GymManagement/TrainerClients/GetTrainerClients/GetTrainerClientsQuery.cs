namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;

public record GetTrainerClientsQuery(int TrainerId, string? Cursor, int? PageSize);