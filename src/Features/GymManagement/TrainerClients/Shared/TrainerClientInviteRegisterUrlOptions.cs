namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

public sealed class TrainerClientInviteRegisterUrlOptions
{
    public const string SectionName = "GymManagement:TrainerClientInvites:RegisterUrl";

    public string BaseUrl { get; init; } = "https://www.youtube.com/";
    public string PayloadQueryParameterName { get; init; } = "payload";
    public string ObfuscationSalt { get; init; } = "shapeup-trainer-invite-v1";
}

