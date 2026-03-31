namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

public interface ITrainerClientInviteRegisterUrlBuilder
{
    string BuildRegisterUrl(int trainerId, string inviteToken);
}

