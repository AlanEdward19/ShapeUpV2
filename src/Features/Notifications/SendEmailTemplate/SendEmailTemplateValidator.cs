namespace ShapeUp.Features.Notifications.SendEmailTemplate;

using FluentValidation;

public sealed class SendEmailTemplateValidator : AbstractValidator<SendEmailTemplateCommand>
{
    public SendEmailTemplateValidator()
    {
        RuleFor(command => command.To)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.TemplateId)
            .NotEmpty()
            .MaximumLength(120);
    }
}

