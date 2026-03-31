namespace ShapeUp.Features.Notifications.SendEmailTemplate;

public sealed record SendEmailTemplateResponse(string Provider, string ProviderMessageId, string To, string Subject, string TemplateId);

