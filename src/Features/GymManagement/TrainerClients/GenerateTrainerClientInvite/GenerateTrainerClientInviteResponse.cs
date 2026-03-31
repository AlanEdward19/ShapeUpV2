namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

public record GenerateTrainerClientInviteResponse(
    int InviteId,
    int TrainerId,
    string ClientEmail,
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Status);

