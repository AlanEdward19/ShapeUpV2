namespace ShapeUp.Features.Notifications.SendEmailHtml;

using FluentValidation;

public sealed class SendEmailHtmlValidator : AbstractValidator<SendEmailHtmlCommand>
{
    public SendEmailHtmlValidator()
    {
        RuleFor(command => command.To)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Subject)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Html)
            .NotEmpty();
    }
}

