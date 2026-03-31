using System.Text.Json;
using ShapeUp.Features.Notifications.SendEmailTemplate;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Notifications;

public class SendEmailTemplateHandlerTests
{
    private readonly Mock<IEmailNotificationSender> _sender = new();

    [Fact]
    public async Task HandleAsync_ValidCommand_MapsTemplateVariables()
    {
        _sender
            .Setup(sender => sender.SendTemplateAsync(It.IsAny<SendTemplateEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("provider-template-id")));

        var variables = new Dictionary<string, JsonElement>
        {
            ["firstName"] = JsonDocument.Parse("\"Alan\"").RootElement.Clone(),
            ["attempts"] = JsonDocument.Parse("3").RootElement.Clone()
        };

        var handler = new SendEmailTemplateHandler(_sender.Object, new SendEmailTemplateValidator());
        var result = await handler.HandleAsync(
            new SendEmailTemplateCommand("user@test.com", "Subject", Guid.NewGuid().ToString(), variables),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal("provider-template-id", result.Value!.ProviderMessageId);

        _sender.Verify(
            sender => sender.SendTemplateAsync(
                It.Is<SendTemplateEmailRequest>(request =>
                    request.TemplateId == result.Value.TemplateId &&
                    request.Variables.ContainsKey("firstName") &&
                    request.Variables.ContainsKey("attempts")),
                default),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsValidationError()
    {
        var handler = new SendEmailTemplateHandler(_sender.Object, new SendEmailTemplateValidator());
        var result = await handler.HandleAsync(new SendEmailTemplateCommand("", "", "", null), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
        _sender.Verify(sender => sender.SendTemplateAsync(It.IsAny<SendTemplateEmailRequest>(), default), Times.Never);
    }
}


