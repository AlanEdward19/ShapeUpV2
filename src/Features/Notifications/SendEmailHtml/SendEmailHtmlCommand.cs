namespace ShapeUp.Features.Notifications.SendEmailHtml;

public sealed record SendEmailHtmlCommand(string To, string Subject, string Html);

