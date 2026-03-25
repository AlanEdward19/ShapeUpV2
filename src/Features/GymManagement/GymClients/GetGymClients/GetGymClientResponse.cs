namespace ShapeUp.Features.GymManagement.GymClients.GetGymClients;

public record GetGymClientResponse(int Id, int GymId, int UserId, string PlanName, int? TrainerId, DateTime EnrolledAt);