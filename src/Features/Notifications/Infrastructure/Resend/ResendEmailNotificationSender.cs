namespace ShapeUp.Features.Notifications.Infrastructure.Resend;

using System.Net;
using Microsoft.Extensions.Options;
using global::Resend;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Errors;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Notifications.Shared.Options;
using ShapeUp.Shared.Results;

public sealed class ResendEmailNotificationSender(
    IResend resend,
    IOptions<ResendEmailOptions> options,
    ILogger<ResendEmailNotificationSender> logger) : IEmailNotificationSender
{
    public Task<Result<EmailDispatchReceipt>> SendHtmlAsync(SendHtmlEmailRequest request, CancellationToken cancellationToken)
    {
        var message = CreateBaseMessage(request.To, request.Subject);
        message.HtmlBody = request.Html;

        return SendAsync(message, cancellationToken);
    }

    public Task<Result<EmailDispatchReceipt>> SendTemplateAsync(SendTemplateEmailRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.TemplateId, out var templateId))
            return Task.FromResult(Result<EmailDispatchReceipt>.Failure(NotificationErrors.InvalidTemplateId(request.TemplateId)));

        var message = CreateBaseMessage(request.To, request.Subject);
        message.Template = new EmailMessageTemplate
        {
            TemplateId = templateId,
            Variables = request.Variables.ToDictionary(item => item.Key, item => item.Value!)
        };

        return SendAsync(message, cancellationToken);
    }

    private async Task<Result<EmailDispatchReceipt>> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var optionsValue = options.Value;
        if (string.IsNullOrWhiteSpace(optionsValue.ApiToken))
            return Result<EmailDispatchReceipt>.Failure(NotificationErrors.ProviderConfigurationMissing($"{ResendEmailOptions.SectionName}:ApiToken"));

        if (string.IsNullOrWhiteSpace(optionsValue.FromEmail))
            return Result<EmailDispatchReceipt>.Failure(NotificationErrors.ProviderConfigurationMissing($"{ResendEmailOptions.SectionName}:FromEmail"));

        try
        {
            var providerMessageId = await resend.EmailSendAsync(message, cancellationToken);
            return Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(providerMessageId.ToString()!));
        }
        catch (ResendException exception)
        {
            logger.LogWarning(exception, "Resend rejected an email notification request.");

            var statusCode = exception.StatusCode.HasValue
                ? (int)exception.StatusCode.Value
                : StatusCodes.Status502BadGateway;

            return Result<EmailDispatchReceipt>.Failure(NotificationErrors.ProviderFailure(exception.Message, statusCode));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unexpected error while sending email notification through Resend.");
            return Result<EmailDispatchReceipt>.Failure(
                NotificationErrors.ProviderFailure("The notification provider is temporarily unavailable.", StatusCodes.Status502BadGateway));
        }
    }

    private static string BuildFrom(ResendEmailOptions options) =>
        string.IsNullOrWhiteSpace(options.FromName)
            ? options.FromEmail!
            : $"{options.FromName} <{options.FromEmail}>";

    private EmailMessage CreateBaseMessage(string to, string subject)
    {
        var optionsValue = options.Value;
        var message = new EmailMessage
        {
            From = BuildFrom(optionsValue),
            Subject = subject
        };

        if (!string.IsNullOrWhiteSpace(optionsValue.ReplyTo))
            message.ReplyTo = optionsValue.ReplyTo;

        message.To.Add(to);
        return message;
    }
}



