namespace ShapeUp.Features.Notifications.Shared.Options;

public sealed class ResendEmailOptions
{
    public const string SectionName = "Notifications:Resend";

    public string? ApiToken { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string? ReplyTo { get; init; }
}

