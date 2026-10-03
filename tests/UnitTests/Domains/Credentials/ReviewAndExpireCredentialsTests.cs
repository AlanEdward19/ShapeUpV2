using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using ShapeUp.Features.Credentials;
using ShapeUp.Features.Credentials.ExpireCredentials;
using ShapeUp.Features.Credentials.ReviewCredential;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.Credentials;

public class ReviewAndExpireCredentialsTests
{
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly CredentialLifecycle _lifecycle;

    public ReviewAndExpireCredentialsTests()
    {
        _lifecycle = new CredentialLifecycle(_credentials.Object, new ProfessionalRoleGranter(_roles.Object, _credentials.Object));
    }

    private static ProfessionalCredential Credential(int id, CredentialStatus status, int userId = 7, DateTime? expiresAt = null) => new()
    {
        Id = id, UserId = userId, ProfessionType = "PersonalTrainer", CredentialNumber = "1", IssuingAuthority = "CREF",
        IssuingRegion = "SP", Country = "BR", Status = status, ExpiresAt = expiresAt
    };

    private ReviewCredentialHandler Review() => new(_credentials.Object, _lifecycle, new RejectCredentialValidator());

    [Fact]
    public async Task Approve_UnderReview_VerifiesAndGrantsRole()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));

        var result = await Review().ApproveAsync(5, 99, default);

        Assert.Equal("Verified", result.Value!.Status);
        _roles.Verify(r => r.AddAsync(It.Is<UserPlatformRole>(x => x.Role == PlatformRoleType.Trainer && x.GrantedByCredentialId == 5), default), Times.Once);
    }

    [Fact]
    public async Task Approve_NotFound_Returns404()
    {
        var result = await Review().ApproveAsync(5, 99, default);

        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Approve_AlreadyRejected_ReturnsConflict()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.Rejected));

        var result = await Review().ApproveAsync(5, 99, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Reject_WithReason_SavesReason()
    {
        _credentials.Setup(c => c.GetByIdAsync(5, default)).ReturnsAsync(Credential(5, CredentialStatus.UnderReview));

        var result = await Review().RejectAsync(5, new RejectCredentialCommand(" Registro inativo "), 99, default);

        Assert.Equal("Rejected", result.Value!.Status);
        Assert.Equal("Registro inativo", result.Value.RejectionReason);
    }

    [Fact]
    public async Task Reject_WithoutReason_ReturnsValidationError()
    {
        var result = await Review().RejectAsync(5, new RejectCredentialCommand(""), 99, default);

        Assert.Equal(400, result.Error!.StatusCode);
        _credentials.Verify(c => c.GetByIdAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task Expire_ExpiresDueCredentialsAndRemovesOnlyCredentialGrantedRole()
    {
        var granted = Credential(1, CredentialStatus.Verified, userId: 7, expiresAt: Now.AddDays(-1));
        var manual = Credential(2, CredentialStatus.Verified, userId: 8, expiresAt: Now.AddDays(-1));
        _credentials.Setup(c => c.GetExpiredVerifiedAsync(Now, default)).ReturnsAsync([granted, manual]);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 30, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = 1 });
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(8, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 31, UserId = 8, Role = PlatformRoleType.Trainer });

        var expired = await new ExpireCredentialsHandler(_credentials.Object, _lifecycle).HandleAsync(Now, default);

        Assert.Equal(2, expired);
        Assert.Equal(CredentialStatus.Expired, granted.Status);
        Assert.Equal(CredentialStatus.Expired, manual.Status);
        _roles.Verify(r => r.DeleteAsync(30, default), Times.Once);
        _roles.Verify(r => r.DeleteAsync(31, default), Times.Never);
    }

    [Fact]
    public async Task Expire_NothingDue_ReturnsZero()
    {
        _credentials.Setup(c => c.GetExpiredVerifiedAsync(Now, default)).ReturnsAsync([]);

        var expired = await new ExpireCredentialsHandler(_credentials.Object, _lifecycle).HandleAsync(Now, default);

        Assert.Equal(0, expired);
    }

    [Theory]
    [InlineData(nameof(CredentialsController.GetUnderReview))]
    [InlineData(nameof(CredentialsController.Approve))]
    [InlineData(nameof(CredentialsController.Reject))]
    public void AdminEndpoints_RequireCredentialsReviewPolicy(string action)
    {
        var attribute = typeof(CredentialsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.Equal("capability:platform.credentials.review", attribute?.Policy);
    }

    [Theory]
    [InlineData(nameof(CredentialsController.Submit))]
    [InlineData(nameof(CredentialsController.GetMine))]
    public void SelfServiceEndpoints_DoNotRequireAdminPolicy(string action)
    {
        var attribute = typeof(CredentialsController).GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.Null(attribute);
    }
}
