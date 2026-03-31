namespace ShapeUp.Features.Notifications.Shared.Models;

public sealed record SendHtmlEmailRequest(string To, string Subject, string Html);

