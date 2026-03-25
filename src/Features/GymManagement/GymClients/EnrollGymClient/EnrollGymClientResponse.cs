namespace ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;

public record EnrollGymClientResponse(int Id, int GymId, int UserId, int GymPlanId, int? TrainerId, DateTime EnrolledAt);