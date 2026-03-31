namespace ShapeUp.Features.GymManagement.TrainerClients.AcceptTrainerClientInvite;

public record AcceptTrainerClientInviteResponse(
    int TrainerClientId,
    int TrainerId,
    int ClientId,
    int? TrainerPlanId,
    DateTime EnrolledAt);

