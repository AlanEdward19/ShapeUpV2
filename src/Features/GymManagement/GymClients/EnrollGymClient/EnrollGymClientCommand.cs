namespace ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;

public record EnrollGymClientCommand(int GymId, int UserId, int GymPlanId, int? TrainerId);

