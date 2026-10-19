using Microsoft.Extensions.Options;
using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.Clients;

public class NutritionInviteEmailTests
{
    private readonly Mock<IProfessionalCapabilityService> _capabilities = new();
    private readonly Mock<IProfessionalClientInviteRepository> _invites = new();
    private readonly Mock<IEmailNotificationSender> _sender = new();
    private readonly Mock<IUserRepository> _users = new();

    public NutritionInviteEmailTests()
    {
        _capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        _users.Setup(u => u.GetByIdAsync(1, default)).ReturnsAsync(new User { Id = 1, FirebaseUid = "f", Email = "n@x.com", DisplayName = "Dra. Bia" });
        _sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("id")));
    }

    private InviteNutritionClientHandler Handler() => new(
        _capabilities.Object, _invites.Object, null, _sender.Object, _users.Object,
        Options.Create(new NutritionInviteLinkOptions { BaseUrl = "https://shapeup.test/register" }));

    [Fact]
    public async Task WithEmail_SendsLinkThatOpensTheAppWithTheToken()
    {
        SendHtmlEmailRequest? sent = null;
        _sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .Callback<SendHtmlEmailRequest, CancellationToken>((r, _) => sent = r)
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("id")));
        ProfessionalClientInvite? stored = null;
        _invites.Setup(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), default))
            .Callback<ProfessionalClientInvite, CancellationToken>((i, _) => stored = i);

        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, "  Aluno@X.com "), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.EmailSent);
        Assert.Equal("aluno@x.com", result.Value.ClientEmail);
        Assert.Equal("aluno@x.com", sent!.To);
        Assert.Equal($"https://shapeup.test/register?nutrition={Uri.EscapeDataString(result.Value.Token)}", result.Value.InviteUrl);
        Assert.Contains(result.Value.InviteUrl!, sent.Html.Replace("&amp;", "&"));
        Assert.Contains("Dra. Bia", sent.Subject);
        Assert.Equal("aluno@x.com", stored!.InviteeEmail);
    }

    [Fact]
    public async Task WithoutEmail_NoMessageIsSent_ButLinkStillReturned()
    {
        var result = await Handler().HandleAsync(1, null, default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.EmailSent);
        Assert.Null(result.Value.ClientEmail);
        Assert.Contains("nutrition=", result.Value.InviteUrl);
        _sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProviderFailure_KeepsTheInviteAndReportsIt()
    {
        _sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Failure(new Error("email_failed", "secret-token-leak", 502)));

        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, "a@x.com"), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.EmailSent);
        Assert.False(string.IsNullOrEmpty(result.Value.EmailError));
        Assert.DoesNotContain("secret", result.Value.EmailError);
        _invites.Verify(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), default), Times.Once);
    }

    [Fact]
    public async Task SenderThrows_StillReturnsTheInvite()
    {
        _sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default)).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, "a@x.com"), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.EmailSent);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("a b@x.com")]
    public async Task InvalidEmail_IsRejectedBeforeAnythingIsSaved(string email)
    {
        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, email), default);

        Assert.Equal(400, result.Error!.StatusCode);
        _invites.Verify(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), default), Times.Never);
    }

    [Fact]
    public async Task ResendingToTheSameEmail_RevokesThePreviousInvite_EvenAtTheCap()
    {
        var previous = new ProfessionalClientInvite
        {
            Id = 9, ProfessionalUserId = 1, RelationshipType = "Nutrition", TokenHash = "h", InviteeEmail = "a@x.com",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        _invites.Setup(i => i.GetPendingByEmailAsync(1, "Nutrition", "a@x.com", It.IsAny<DateTime>(), default)).ReturnsAsync(previous);
        _invites.Setup(i => i.CountPendingAsync(1, "Nutrition", It.IsAny<DateTime>(), default)).ReturnsAsync(InviteNutritionClientHandler.MaxPendingInvites);
        _invites.Setup(i => i.TryRevokeAsync(9, 1, default)).ReturnsAsync(true);

        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, "a@x.com"), default);

        Assert.True(result.IsSuccess);
        _invites.Verify(i => i.TryRevokeAsync(9, 1, default), Times.Once);
    }

    [Fact]
    public async Task ANewEmailAtTheCap_IsStillRefused()
    {
        _invites.Setup(i => i.CountPendingAsync(1, "Nutrition", It.IsAny<DateTime>(), default)).ReturnsAsync(InviteNutritionClientHandler.MaxPendingInvites);

        var result = await Handler().HandleAsync(1, new InviteNutritionClientCommand(null, "new@x.com"), default);

        Assert.Equal(409, result.Error!.StatusCode);
    }
}
