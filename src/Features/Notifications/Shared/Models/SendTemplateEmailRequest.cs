namespace ShapeUp.Features.Notifications.Shared.Models;

public sealed record SendTemplateEmailRequest(
    string To,
    string Subject,
    string TemplateId,
    IReadOnlyDictionary<string, object?> Variables);

