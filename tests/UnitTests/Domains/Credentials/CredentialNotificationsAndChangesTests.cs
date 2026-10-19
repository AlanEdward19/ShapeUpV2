using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Credentials.Infrastructure.Notifications;
using ShapeUp.Features.Credentials.Infrastructure.Verification;
using ShapeUp.Features.Credentials.ResubmitCredential;
using ShapeUp.Features.Credentials.ReviewCredential;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.Credentials.Shared.Notifications;
using ShapeUp.Features.Credentials.Shared.StateMachine;
using ShapeUp.Features.Credentials.Shared.Verification;
using ShapeUp.Features.Credentials.SubmitCredential;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Credentials;

public class CredentialNotificationsAndChangesTests
{
    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly Mock<ICredentialNotifier> _notifier = new();
    private readonly CredentialLifecycle _lifecycle;

    public CredentialNotificationsAndChangesTests()
    {
        _lifecycle = new CredentialLifecycle(
            _credentials.Object, new ProfessionalRoleGranter(_roles.Object, _credentials.Object), null, _notifier.Object);
        _credentials.Setup(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default))
            .Callback<ProfessionalCredential, CancellationToken>((c, _) => c.Id = 1)
            .Returns(Task.CompletedTask);
    }

    private static ProfessionalCredential Credential(CredentialStatus status, int userId = 7) => new()
    {
        Id = 5, UserId = userId, ProfessionType = "PersonalTrainer", CredentialNumber = "123", IssuingAuthority = "CREF",
        IssuingRegion = "SP", Country = "BR", Status = status
    };

    // State machine ------------------------------------------------------------------------------

    [Theory]
    [InlineData(CredentialStatus.UnderReview, CredentialStatus.NeedsChanges, true)]
    [InlineData(CredentialStatus.NeedsChanges, CredentialStatus.Submitted, true)]
    [InlineData(CredentialStatus.NeedsChanges, CredentialStatus.Rejected, true)]
    [InlineData(CredentialStatus.NeedsChanges, CredentialStatus.Verified, false)]
    [InlineData(CredentialStatus.Verified, CredentialStatus.NeedsChanges, false)]
    [InlineData(CredentialStatus.Submitted, CredentialStatus.NeedsChanges, false)]
    public void Guard_NeedsChangesTransitions(CredentialStatus from, CredentialStatus to, bool allowed) =>
        Assert.Equal(allowed, CredentialStatusGuard.IsValidTransition(from, to));

    // One e-mail per transition ------------------------------------------------------------------

    [Fact]
    public async Task Approve_SendsApprovedEmailOnce()
    {
        var credential = Credential(CredentialStatus.UnderReview);

        var result = await _lifecycle.ApproveAsync(credential, 1, default);

        Assert.True(result.IsSuccess);
        _notifier.Verify(n => n.ApprovedAsync(credential, default), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Approve_InvalidTransition_SendsNothing()
    {
        var result = await _lifecycle.ApproveAsync(Credential(CredentialStatus.Rejected), 1, default);

        Assert.True(result.IsFailure);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Approve_ConcurrentChange_SendsNothing()
    {
        _credentials.Setup(c => c.UpdateAsync(It.IsAny<ProfessionalCredential>(), default))
            .ThrowsAsync(new ShapeUp.Features.Credentials.Shared.Errors.CredentialConflictException(
                ShapeUp.Features.Credentials.Shared.Errors.CredentialConflictKind.ConcurrentChange, new Exception()));

        var result = await _lifecycle.ApproveAsync(Credential(CredentialStatus.UnderReview), 1, default);

        Assert.True(result.IsFailure);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reject_SendsRejectedEmailWithReason()
    {
        var credential = Credential(CredentialStatus.UnderReview);

        await _lifecycle.RejectAsync(credential, "Número inválido", 1, default);

        _notifier.Verify(n => n.RejectedAsync(It.Is<ProfessionalCredential>(c => c.RejectionReason == "Número inválido"), default), Times.Once);
    }

    [Fact]
    public async Task RequestChanges_StoresMessageAndSendsChangesEmail()
    {
        var credential = Credential(CredentialStatus.UnderReview);

        var result = await _lifecycle.RequestChangesAsync(credential, "Confira o número", 1, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.NeedsChanges, credential.Status);
        Assert.Equal("Confira o número", credential.ChangesRequestedMessage);
        Assert.NotNull(credential.ChangesRequestedAt);
        _notifier.Verify(n => n.ChangesRequestedAsync(credential, default), Times.Once);
    }

    [Theory]
    [InlineData(CredentialStatus.Verified)]
    [InlineData(CredentialStatus.Rejected)]
    [InlineData(CredentialStatus.NeedsChanges)]
    public async Task RequestChanges_OnlyFromUnderReview(CredentialStatus status)
    {
        var result = await _lifecycle.RequestChangesAsync(Credential(status), "x", 1, default);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
        _notifier.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(CredentialStatus.Expired)]
    [InlineData(CredentialStatus.Suspended)]
    [InlineData(CredentialStatus.Revoked)]
    public async Task End_SendsAccessEndedEmail(CredentialStatus target)
    {
        var credential = Credential(CredentialStatus.Verified);

        var result = await _lifecycle.EndAsync(credential, target, DateTime.UtcNow, default, "motivo", 1);

        Assert.True(result.IsSuccess);
        _notifier.Verify(n => n.AccessEndedAsync(credential, default), Times.Once);
    }

    // Submit / resubmit --------------------------------------------------------------------------

    [Fact]
    public async Task Submit_StaysInReview_SendsReceivedEmailOnly()
    {
        var handler = new SubmitCredentialHandler(
            _credentials.Object, _lifecycle, [new ManualReviewCredentialVerifier()], new SubmitCredentialValidator(), _notifier.Object);

        var result = await handler.HandleAsync(new SubmitCredentialCommand("PersonalTrainer", "123", "CREF", "SP", "BR"), 7, default);

        Assert.Equal("UnderReview", result.Value!.Status);
        _notifier.Verify(n => n.ReceivedAsync(It.IsAny<ProfessionalCredential>(), default), Times.Once);
        _notifier.Verify(n => n.ApprovedAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    [Fact]
    public async Task Submit_AutoVerified_SendsApprovedNotReceived()
    {
        var verifier = new Mock<ICredentialVerifier>();
        verifier.Setup(v => v.Supports("CREF")).Returns(true);
        verifier.Setup(v => v.VerifyAsync(It.IsAny<ProfessionalCredential>(), default))
            .ReturnsAsync(new CredentialVerificationResult(CredentialVerificationOutcome.Verified));
        var handler = new SubmitCredentialHandler(
            _credentials.Object, _lifecycle, [verifier.Object], new SubmitCredentialValidator(), _notifier.Object);

        var result = await handler.HandleAsync(new SubmitCredentialCommand("PersonalTrainer", "123", "CREF", "SP", "BR"), 7, default);

        Assert.Equal("Verified", result.Value!.Status);
        _notifier.Verify(n => n.ApprovedAsync(It.IsAny<ProfessionalCredential>(), default), Times.Once);
        _notifier.Verify(n => n.ReceivedAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    private ResubmitCredentialHandler Resubmit() =>
        new(_credentials.Object, _lifecycle, [new ManualReviewCredentialVerifier()], new ResubmitCredentialValidator(), _notifier.Object);

    [Fact]
    public async Task Resubmit_ByOwner_ClearsMessageAndReturnsToReview()
    {
        var credential = Credential(CredentialStatus.NeedsChanges);
        credential.ChangesRequestedMessage = "Confira o número";
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(credential);

        var result = await Resubmit().HandleAsync(5, new ResubmitCredentialCommand(" 999-g ", "RJ"), 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("UnderReview", result.Value!.Status);
        Assert.Equal("999-G", credential.CredentialNumber);
        Assert.Equal("RJ", credential.IssuingRegion);
        Assert.Null(credential.ChangesRequestedMessage);
        _notifier.Verify(n => n.ReceivedAsync(credential, default), Times.Once);
    }

    [Fact]
    public async Task Resubmit_ByAnotherUser_IsNotFound()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(CredentialStatus.NeedsChanges, userId: 7));

        var result = await Resubmit().HandleAsync(5, new ResubmitCredentialCommand("999", "SP"), 8, default);

        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Resubmit_NotInNeedsChanges_IsConflict()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(CredentialStatus.UnderReview));

        var result = await Resubmit().HandleAsync(5, new ResubmitCredentialCommand("999", "SP"), 7, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Resubmit_RegistrationUsedByAnother_IsConflict()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(CredentialStatus.NeedsChanges));
        _credentials.Setup(c => c.IsRegistrationInUseAsync("CREF", "SP", "999", default)).ReturnsAsync(true);

        var result = await Resubmit().HandleAsync(5, new ResubmitCredentialCommand("999", "SP"), 7, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Resubmit_InvalidRegion_IsValidationError()
    {
        var result = await Resubmit().HandleAsync(5, new ResubmitCredentialCommand("999", "XX"), 7, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task ReviewHandler_RequestChanges_RequiresMessage()
    {
        var handler = new ReviewCredentialHandler(
            _credentials.Object, _lifecycle, new CredentialRequesterEnricher(Mock.Of<IUserRepository>()),
            new RejectCredentialValidator(), new ApproveCredentialValidator());

        var result = await handler.RequestChangesAsync(5, new RequestChangesCommand(" "), 1, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    // E-mail notifier ----------------------------------------------------------------------------

    private static User UserWithEmail() => new() { Id = 7, FirebaseUid = "f", Email = "ana@x.com", DisplayName = "Ana Souza" };

    [Fact]
    public async Task EmailNotifier_ProviderFails_RetriesAndNeverThrows()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, default)).ReturnsAsync(UserWithEmail());
        var sender = new Mock<IEmailNotificationSender>();
        sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Failure(new Error("email_failed", "down", 502)));

        await new EmailCredentialNotifier(users.Object, sender.Object).ApprovedAsync(Credential(CredentialStatus.Verified), default);

        sender.Verify(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default), Times.Exactly(3));
    }

    [Fact]
    public async Task EmailNotifier_SenderThrows_IsSwallowed()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, default)).ReturnsAsync(UserWithEmail());
        var sender = new Mock<IEmailNotificationSender>();
        sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default)).ThrowsAsync(new InvalidOperationException("boom"));

        await new EmailCredentialNotifier(users.Object, sender.Object).RejectedAsync(Credential(CredentialStatus.Rejected), default);
    }

    [Fact]
    public async Task EmailNotifier_ChangesRequested_EncodesAdminTextAndTargetsOwner()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, default)).ReturnsAsync(UserWithEmail());
        SendHtmlEmailRequest? sent = null;
        var sender = new Mock<IEmailNotificationSender>();
        sender.Setup(s => s.SendHtmlAsync(It.IsAny<SendHtmlEmailRequest>(), default))
            .Callback<SendHtmlEmailRequest, CancellationToken>((r, _) => sent = r)
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("id")));
        var credential = Credential(CredentialStatus.NeedsChanges);
        credential.ChangesRequestedMessage = "<script>x</script> número";

        await new EmailCredentialNotifier(users.Object, sender.Object).ChangesRequestedAsync(credential, default);

        Assert.Equal("ana@x.com", sent!.To);
        Assert.Contains("Ajuste solicitado", sent.Html);
        Assert.DoesNotContain("<script>", sent.Html);
        Assert.Contains("&lt;script&gt;", sent.Html);
    }

    [Fact]
    public async Task EmailNotifier_UserWithoutEmail_SendsNothing()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, default)).ReturnsAsync((User?)null);
        var sender = new Mock<IEmailNotificationSender>();

        await new EmailCredentialNotifier(users.Object, sender.Object).ReceivedAsync(Credential(CredentialStatus.UnderReview), default);

        sender.VerifyNoOtherCalls();
    }
}
