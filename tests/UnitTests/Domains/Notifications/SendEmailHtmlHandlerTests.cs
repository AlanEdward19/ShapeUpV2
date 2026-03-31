using ShapeUp.Features.Notifications.SendEmailHtml;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Notifications;

public class SendEmailHtmlHandlerTests
{
    private readonly Mock<IEmailNotificationSender> _sender = new();

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsAcceptedPayload()
    {
        _sender
            .Setup(sender => sender.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("provider-id")));

        var handler = new SendEmailHtmlHandler(_sender.Object, new SendEmailHtmlValidator());
        var result = await handler.HandleAsync(new SendEmailHtmlCommand("user@test.com", "Welcome", "<h1>Hello</h1>"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("provider-id", result.Value!.ProviderMessageId);
        Assert.Equal("user@test.com", result.Value.To);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsValidationError()
    {
        var handler = new SendEmailHtmlHandler(_sender.Object, new SendEmailHtmlValidator());
        var result = await handler.HandleAsync(new SendEmailHtmlCommand("", "", ""), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
        _sender.Verify(sender => sender.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default), Times.Never);
    }
}


