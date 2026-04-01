namespace ShapeUp.Features.Notifications.SendEmailTemplate;

using FluentValidation;
using Shared.Abstractions;
using Shared.Helpers;
using Shared.Models;
using ShapeUp.Shared.Results;

public sealed class SendEmailTemplateHandler(
    IEmailNotificationSender emailNotificationSender,
    IValidator<SendEmailTemplateCommand> validator)
{
    public async Task<Result<SendEmailTemplateResponse>> HandleAsync(
        SendEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<SendEmailTemplateResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        var sendResult = await emailNotificationSender.SendTemplateAsync(
            new SendTemplateEmailRequest(
                command.To,
                command.Subject,
                command.TemplateId,
                NotificationTemplateVariablesConverter.ToDictionary(command.Variables)),
            cancellationToken);

        if (sendResult.IsFailure)
            return Result<SendEmailTemplateResponse>.Failure(sendResult.Error!);

        return Result<SendEmailTemplateResponse>.Success(
            new SendEmailTemplateResponse("resend", sendResult.Value!.ProviderMessageId, command.To, command.Subject, command.TemplateId));
    }
}

