namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;

/// <summary><paramref name="Status"/> is Invited, Accepted, Revoked or Expired; an unaccepted invite past its deadline is reported as Expired.</summary>
public record GetTrainerClientInviteResponse(
    int InviteId,
    string ClientEmail,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc);
