namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

public sealed class TrainerClientInviteEmailOptions
{
    public const string SectionName = "GymManagement:TrainerClientInvites:Email";

    public string TemplateId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
}

