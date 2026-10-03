using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Credentials.EndCredential;
using ShapeUp.Features.Credentials.ExpireCredentials;
using ShapeUp.Features.Credentials.GetCredentialsUnderReview;
using ShapeUp.Features.Credentials.Infrastructure.Verification;
using ShapeUp.Features.Credentials.ReviewCredential;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Errors;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.Credentials.SubmitCredential;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

namespace UnitTests.Domains.Credentials;

public class CredentialHardeningTests
{
    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly CredentialLifecycle _lifecycle;
    private readonly CredentialRequesterEnricher _enricher;

    public CredentialHardeningTests()
    {
        _lifecycle = new CredentialLifecycle(_credentials.Object, new ProfessionalRoleGranter(_roles.Object, _credentials.Object));
        _enricher = new CredentialRequesterEnricher(_users.Object);
        _users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), default)).ReturnsAsync([]);
        _credentials.Setup(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default))
            .Callback<ProfessionalCredential, CancellationToken>((c, _) => c.Id = 1)
            .Returns(Task.CompletedTask);
    }

    private static ProfessionalCredential Credential(int id, CredentialStatus status, int userId = 7) => new()
    {
        Id = id, UserId = userId, ProfessionType = "PersonalTrainer", CredentialNumber = "1", IssuingAuthority = "CREF",
        IssuingRegion = "SP", Country = "BR", Status = status
    };

    private ReviewCredentialHandler Review() => new(
        _credentials.Object, _lifecycle, _enricher, new RejectCredentialValidator(), new ApproveCredentialValidator());

    private EndCredentialHandler End() => new(_credentials.Object, _lifecycle, _enricher, new EndCredentialValidator());

    private void ExistingRole(int userId, int? grantedBy, int roleId = 30) =>
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = roleId, UserId = userId, Role = PlatformRoleType.Trainer, GrantedByCredentialId = grantedBy });

    // I2 -----------------------------------------------------------------------------------------

    [Fact]
    public async Task UnderReview_ReturnsRequesterNameAndEmail()
    {
        _credentials.Setup(c => c.GetByStatusKeysetAsync(CredentialStatus.UnderReview, null, 21, default))
            .ReturnsAsync([Credential(5, CredentialStatus.UnderReview, userId: 7)]);
        _users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), default))
            .ReturnsAsync([new User { Id = 7, FirebaseUid = "f", Email = "ana@x.com", DisplayName = "Ana" }]);

        var result = await new GetCredentialsUnderReviewHandler(_credentials.Object, _enricher)
            .HandleAsync(new GetCredentialsUnderReviewQuery(null, null), default);

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Ana", item.RequesterName);
        Assert.Equal("ana@x.com", item.RequesterEmail);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task UnderReview_PaginatesWithKeysetCursor()
    {
        _credentials.Setup(c => c.GetByStatusKeysetAsync(CredentialStatus.UnderReview, null, 3, default))
            .ReturnsAsync([Credential(1, CredentialStatus.UnderReview), Credential(2, CredentialStatus.UnderReview), Credential(3, CredentialStatus.UnderReview)]);
        var handler = new GetCredentialsUnderReviewHandler(_credentials.Object, _enricher);

        var first = await handler.HandleAsync(new GetCredentialsUnderReviewQuery(null, 2), default);

        Assert.Equal(2, first.Value!.Items.Length);
        Assert.NotNull(first.Value.NextCursor);

        _credentials.Setup(c => c.GetByStatusKeysetAsync(CredentialStatus.UnderReview, 2, 3, default))
            .ReturnsAsync([Credential(3, CredentialStatus.UnderReview)]);
        var second = await handler.HandleAsync(new GetCredentialsUnderReviewQuery(first.Value.NextCursor, 2), default);

        Assert.Equal(3, Assert.Single(second.Value!.Items).Id);
        Assert.Null(second.Value.NextCursor);
    }

    [Fact]
    public async Task UnderReview_InvalidCursor_ReturnsBadRequest()
    {
        var result = await new GetCredentialsUnderReviewHandler(_credentials.Object, _enricher)
            .HandleAsync(new GetCredentialsUnderReviewQuery("not-a-cursor", null), default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Approve_ResponseCarriesRequester()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview, userId: 7));
        _users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), default))
            .ReturnsAsync([new User { Id = 7, FirebaseUid = "f", Email = "ana@x.com", DisplayName = "Ana" }]);

        var result = await Review().ApproveAsync(5, null, 99, default);

        Assert.Equal("ana@x.com", result.Value!.RequesterEmail);
    }

    [Fact]
    public async Task Submit_RegistrationUsedByAnotherUser_ReturnsConflict()
    {
        _credentials.Setup(c => c.IsRegistrationInUseAsync("CREF", "SP", "123456-G/SP", default)).ReturnsAsync(true);

        var result = await SubmitHandler().HandleAsync(new("PersonalTrainer", "123456-g/sp", "CREF", "SP", "BR"), 7, default);

        Assert.Equal(409, result.Error!.StatusCode);
        _credentials.Verify(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    // I3 -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(CredentialConflictKind.OpenForUserAndProfession)]
    [InlineData(CredentialConflictKind.RegistrationInUse)]
    public async Task Submit_UniqueIndexViolationOnInsert_ReturnsConflict(CredentialConflictKind kind)
    {
        _credentials.Setup(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default)).ThrowsAsync(new CredentialConflictException(kind));

        var result = await SubmitHandler().HandleAsync(new("PersonalTrainer", "123456-G/SP", "CREF", "SP", "BR"), 7, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    private SubmitCredentialHandler SubmitHandler() =>
        new(_credentials.Object, _lifecycle, [new ManualReviewCredentialVerifier()], new SubmitCredentialValidator());

    // I4 -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Approve_LosesTheRace_Returns409AndGrantsNoRole()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));
        _credentials.Setup(c => c.UpdateAsync(It.IsAny<ProfessionalCredential>(), default))
            .ThrowsAsync(new CredentialConflictException(CredentialConflictKind.ConcurrentChange));

        var result = await Review().ApproveAsync(5, null, 99, default);

        Assert.Equal(409, result.Error!.StatusCode);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task Reject_LosesTheRace_Returns409()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));
        _credentials.Setup(c => c.UpdateAsync(It.IsAny<ProfessionalCredential>(), default))
            .ThrowsAsync(new CredentialConflictException(CredentialConflictKind.ConcurrentChange));

        var result = await Review().RejectAsync(5, new RejectCredentialCommand("x"), 99, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Approve_RoleGrantKeepsFailing_UndoesTheApprovalAndReturns503()
    {
        var credential = Credential(5, CredentialStatus.UnderReview);
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(credential);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default)).ThrowsAsync(new InvalidOperationException("db down"));
        _credentials.Setup(c => c.RevertAsync(It.IsAny<ProfessionalCredential>(), CredentialStatus.Verified, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await Review().ApproveAsync(5, null, 99, default);

        Assert.Equal(503, result.Error!.StatusCode);
        Assert.Equal(CredentialStatus.UnderReview, credential.Status);
        _roles.Verify(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default), Times.Exactly(3));
        _credentials.Verify(c => c.RevertAsync(
            It.Is<ProfessionalCredential>(s => s.Id == 5 && s.Status == CredentialStatus.UnderReview && s.VerifiedAt == null && s.ExpiresAt == null),
            CredentialStatus.Verified, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_RoleGrantFailsOnceThenWorks_Succeeds()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));
        _roles.SetupSequence(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ThrowsAsync(new InvalidOperationException("blip"))
            .ReturnsAsync((UserPlatformRole?)null);

        var result = await Review().ApproveAsync(5, null, 99, default);

        Assert.Equal("Verified", result.Value!.Status);
        _roles.Verify(r => r.AddAsync(It.Is<UserPlatformRole>(x => x.GrantedByCredentialId == 5), default), Times.Once);
        _credentials.Verify(c => c.RevertAsync(It.IsAny<ProfessionalCredential>(), It.IsAny<CredentialStatus>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // I5 -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Approve_WithoutExpiresAt_DefaultsToTwelveMonths()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));

        var result = await Review().ApproveAsync(5, null, 99, default);

        var expected = DateTime.UtcNow.AddMonths(12);
        Assert.InRange(result.Value!.ExpiresAt!.Value, expected.AddMinutes(-1), expected.AddMinutes(1));
    }

    [Fact]
    public async Task Approve_WithExpiresAt_UsesIt()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));
        var date = DateTime.UtcNow.AddMonths(3);

        var result = await Review().ApproveAsync(5, new ApproveCredentialCommand(date), 99, default);

        Assert.Equal(date, result.Value!.ExpiresAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6 * 365)]
    public async Task Approve_ExpiresAtOutOfRange_ReturnsBadRequest(int daysFromNow)
    {
        var result = await Review().ApproveAsync(5, new ApproveCredentialCommand(DateTime.UtcNow.AddDays(daysFromNow)), 99, default);

        Assert.Equal(400, result.Error!.StatusCode);
        _credentials.Verify(c => c.GetByIdAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Theory]
    [InlineData(CredentialStatus.Suspended)]
    [InlineData(CredentialStatus.Revoked)]
    public async Task SuspendOrRevoke_VerifiedCredential_EndsItAndWithdrawsGrantedRole(CredentialStatus target)
    {
        var credential = Credential(5, CredentialStatus.Verified);
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(credential);
        ExistingRole(7, grantedBy: 5);

        var command = new EndCredentialCommand(" Registro cancelado ");
        var result = target == CredentialStatus.Suspended
            ? await End().SuspendAsync(5, command, 99, default)
            : await End().RevokeAsync(5, command, 99, default);

        Assert.Equal(target.ToString(), result.Value!.Status);
        Assert.Equal("Registro cancelado", result.Value.EndReason);
        Assert.NotNull(result.Value.EndedAt);
        Assert.Equal(99, credential.EndedByUserId);
        _roles.Verify(r => r.DeleteAsync(30, default), Times.Once);
    }

    [Fact]
    public async Task Suspend_KeepsRoleAssignedByAnAdmin()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.Verified));
        ExistingRole(7, grantedBy: null);

        await End().SuspendAsync(5, new EndCredentialCommand("x"), 99, default);

        _roles.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task Suspend_NotVerified_ReturnsConflict()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));

        var result = await End().SuspendAsync(5, new EndCredentialCommand("x"), 99, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Revoke_WithoutReason_ReturnsBadRequest()
    {
        var result = await End().RevokeAsync(5, new EndCredentialCommand(""), 99, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Revoke_RoleRemovalFails_RestoresVerifiedAndReturns503()
    {
        var credential = Credential(5, CredentialStatus.Verified);
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(credential);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default)).ThrowsAsync(new InvalidOperationException("db down"));
        _credentials.Setup(c => c.RevertAsync(It.IsAny<ProfessionalCredential>(), CredentialStatus.Revoked, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await End().RevokeAsync(5, new EndCredentialCommand("x"), 99, default);

        Assert.Equal(503, result.Error!.StatusCode);
        Assert.Equal(CredentialStatus.Verified, credential.Status);
        _credentials.Verify(c => c.RevertAsync(
            It.Is<ProfessionalCredential>(s => s.Status == CredentialStatus.Verified), CredentialStatus.Revoked, It.IsAny<CancellationToken>()), Times.Once);
    }

    // M6 -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Expire_OneCredentialFails_RestOfTheBatchStillExpires()
    {
        var now = DateTime.UtcNow;
        var broken = Credential(1, CredentialStatus.Verified, userId: 7);
        var fine = Credential(2, CredentialStatus.Verified, userId: 8);
        _credentials.Setup(c => c.GetExpiredVerifiedAsync(now, default)).ReturnsAsync([broken, fine]);
        _credentials.Setup(c => c.UpdateAsync(broken, default)).ThrowsAsync(new InvalidOperationException("boom"));

        var expired = await new ExpireCredentialsHandler(_credentials.Object, _lifecycle).HandleAsync(now, default);

        Assert.Equal(1, expired);
        Assert.Equal(CredentialStatus.Expired, fine.Status);
    }

    // M1 -----------------------------------------------------------------------------------------

    private AssignUserRoleHandler Assign() =>
        new(_roles.Object, new Mock<IPlatformTierRepository>().Object, new AssignUserRoleValidator());

    [Fact]
    public async Task AssignUserRole_RoleGrantedByCredential_IsAdoptedByTheAdmin()
    {
        ExistingRole(7, grantedBy: 5);

        var result = await Assign().HandleAsync(new AssignUserRoleCommand(7, PlatformRoleType.Trainer, null), default);

        Assert.True(result.IsSuccess);
        _roles.Verify(r => r.UpdateAsync(It.Is<UserPlatformRole>(x => x.Id == 30 && x.GrantedByCredentialId == null && x.IsActive), default), Times.Once);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task AssignUserRole_RoleAlreadyAssignedByHand_StillReturnsConflict()
    {
        ExistingRole(7, grantedBy: null);

        var result = await Assign().HandleAsync(new AssignUserRoleCommand(7, PlatformRoleType.Trainer, null), default);

        Assert.Equal(409, result.Error!.StatusCode);
    }
}
