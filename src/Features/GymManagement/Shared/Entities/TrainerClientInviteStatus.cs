namespace ShapeUp.Features.GymManagement.Shared.Entities;

public enum TrainerClientInviteStatus
{
    Invited = 1,
    Accepted = 2,
    Revoked = 3,
    Expired = 4,

    /// <summary>Replaced by a newer invite to the same e-mail (the trainer did not deactivate it by hand).</summary>
    Superseded = 5
}

