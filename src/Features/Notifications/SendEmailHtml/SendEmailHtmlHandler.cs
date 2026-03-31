namespace ShapeUp.Features.Notifications.SendEmailHtml;

using FluentValidation;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

public sealed class SendEmailHtmlHandler(
    IEmailNotificationSender emailNotificationSender,
    IValidator<SendEmailHtmlCommand> validator)
{
    public async Task<Result<SendEmailHtmlResponse>> HandleAsync(
        SendEmailHtmlCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<SendEmailHtmlResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        var sendResult = await emailNotificationSender.SendHtmlAsync(
            new SendHtmlEmailRequest(command.To, command.Subject, command.Html),
            cancellationToken);

        if (sendResult.IsFailure)
            return Result<SendEmailHtmlResponse>.Failure(sendResult.Error!);

        return Result<SendEmailHtmlResponse>.Success(
            new SendEmailHtmlResponse("resend", sendResult.Value!.ProviderMessageId, command.To, command.Subject));
    }
}

