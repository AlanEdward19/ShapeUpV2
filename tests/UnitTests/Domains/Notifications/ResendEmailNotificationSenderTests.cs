namespace UnitTests.Domains.Notifications;

using global::Resend;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Notifications.Infrastructure.Resend;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Notifications.Shared.Options;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;

public sealed class ResendEmailNotificationSenderTests
{
    private const string EmailEnabledFeatureFlagKey = "notifications.email-enabled";

    private readonly Mock<IResend> _resend = new();
    private readonly Mock<IFeatureFlagReader> _featureFlags = new();
    private readonly Mock<ILogger<ResendEmailNotificationSender>> _logger = new();

    private static IOptions<ResendEmailOptions> ValidOptions() =>
        Options.Create(new ResendEmailOptions
        {
            ApiToken = "token",
            FromEmail = "from@test.com"
        });

    private ResendEmailNotificationSender CreateSender() =>
        new(_resend.Object, ValidOptions(), _featureFlags.Object, _logger.Object);

    [Fact]
    public async Task SendHtmlAsync_WhenEmailFlagEnabled_CallsResend()
    {
        _featureFlags
            .Setup(reader => reader.IsEnabledAsync(EmailEnabledFeatureFlagKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var providerMessageId = Guid.NewGuid();
        var resendResponse = new ResendResponse<Guid>(providerMessageId, null);
        _resend
            .Setup(client => client.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resendResponse);

        var sender = CreateSender();
        var result = await sender.SendHtmlAsync(
            new SendHtmlEmailRequest("to@test.com", "Subject", "<p>Hi</p>"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(resendResponse.ToString(), result.Value!.ProviderMessageId);
        _resend.Verify(
            client => client.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendHtmlAsync_WhenEmailFlagDisabled_DoesNotCallResendAndReturnsSuccess()
    {
        _featureFlags
            .Setup(reader => reader.IsEnabledAsync(EmailEnabledFeatureFlagKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sender = CreateSender();
        var result = await sender.SendHtmlAsync(
            new SendHtmlEmailRequest("to@test.com", "Subject", "<p>Hi</p>"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Value!.ProviderMessageId);
        _resend.Verify(
            client => client.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
