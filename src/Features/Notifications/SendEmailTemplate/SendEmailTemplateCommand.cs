namespace ShapeUp.Features.Notifications.SendEmailTemplate;

using System.Text.Json;

public sealed record SendEmailTemplateCommand(
    string To,
    string Subject,
    string TemplateId,
    Dictionary<string, JsonElement>? Variables);

