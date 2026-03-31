namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

using ShapeUp.Shared.Results;

public interface ITrainerClientInvitePayloadCodec
{
    string Encode(TrainerClientInviteUrlPayload payload);
    Result<TrainerClientInviteUrlPayload> Decode(string encodedPayload);
}

