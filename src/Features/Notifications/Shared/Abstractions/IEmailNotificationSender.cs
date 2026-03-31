namespace ShapeUp.Features.Notifications.Shared.Abstractions;

using Models;
using ShapeUp.Shared.Results;

public interface IEmailNotificationSender
{
    Task<Result<EmailDispatchReceipt>> SendHtmlAsync(SendHtmlEmailRequest request, CancellationToken cancellationToken);
    Task<Result<EmailDispatchReceipt>> SendTemplateAsync(SendTemplateEmailRequest request, CancellationToken cancellationToken);
}

