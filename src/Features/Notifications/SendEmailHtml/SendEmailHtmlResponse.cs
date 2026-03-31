namespace ShapeUp.Features.Notifications.SendEmailHtml;

public sealed record SendEmailHtmlResponse(string Provider, string ProviderMessageId, string To, string Subject);

