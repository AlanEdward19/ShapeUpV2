namespace ShapeUp.Features.GymManagement.GymClients.GetGymClients;

public record GetGymClientsQuery(int GymId, string? Cursor, int? PageSize, int? TrainerId);