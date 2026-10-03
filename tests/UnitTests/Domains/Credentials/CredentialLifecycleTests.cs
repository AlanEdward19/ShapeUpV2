using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.Credentials;

public class CredentialLifecycleTests
{
    private static readonly DateTime Now = new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly CredentialLifecycle _lifecycle;

    public CredentialLifecycleTests()
    {
        _lifecycle = new CredentialLifecycle(_credentials.Object, new ProfessionalRoleGranter(_roles.Object, _credentials.Object));
    }

    private static ProfessionalCredential Credential(
        CredentialStatus status,
        string profession = "PersonalTrainer",
        int id = 10,
        int userId = 7) => new()
    {
        Id = id,
        UserId = userId,
        ProfessionType = profession,
        CredentialNumber = "123456-G/SP",
        IssuingAuthority = profession == "PersonalTrainer" ? "CREF" : "CRN",
        IssuingRegion = "SP",
        Country = "BR",
        Status = status
    };

    [Fact]
    public async Task SendToReview_FromSubmitted_MovesToUnderReview()
    {
        var credential = Credential(CredentialStatus.Submitted);

        var result = await _lifecycle.SendToReviewAsync(credential, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.UnderReview, credential.Status);
        _credentials.Verify(c => c.UpdateAsync(credential, default), Times.Once);
    }

    [Fact]
    public async Task SendToReview_FromVerified_FailsWithoutSaving()
    {
        var credential = Credential(CredentialStatus.Verified);

        var result = await _lifecycle.SendToReviewAsync(credential, default);

        Assert.True(result.IsFailure);
        Assert.Equal(CredentialStatus.Verified, credential.Status);
        _credentials.Verify(c => c.UpdateAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    [Theory]
    [InlineData("PersonalTrainer", PlatformRoleType.Trainer)]
    [InlineData("Nutritionist", PlatformRoleType.Nutritionist)]
    public async Task Approve_GrantsRoleMarkedAsGrantedByCredential(string profession, PlatformRoleType expectedRole)
    {
        var credential = Credential(CredentialStatus.UnderReview, profession);
        UserPlatformRole? added = null;
        _roles.Setup(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default))
            .Callback<UserPlatformRole, CancellationToken>((role, _) => added = role)
            .Returns(Task.CompletedTask);

        var result = await _lifecycle.ApproveAsync(credential, 99, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Verified, credential.Status);
        Assert.NotNull(credential.VerifiedAt);
        Assert.Equal(99, credential.ReviewedByUserId);
        Assert.NotNull(added);
        Assert.Equal(7, added.UserId);
        Assert.Equal(expectedRole, added.Role);
        Assert.Equal(10, added.GrantedByCredentialId);
    }

    [Fact]
    public async Task Approve_RoleAlreadyAssignedByHand_DoesNotAddOrTakeOverRole()
    {
        var credential = Credential(CredentialStatus.UnderReview);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer });

        var result = await _lifecycle.ApproveAsync(credential, 99, default);

        Assert.True(result.IsSuccess);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
        _roles.Verify(r => r.UpdateAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task Approve_CalledTwice_IsIdempotentAndSecondCallFailsTransition()
    {
        var credential = Credential(CredentialStatus.UnderReview);
        UserPlatformRole? stored = null;
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default)).ReturnsAsync(() => stored);
        _roles.Setup(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default))
            .Callback<UserPlatformRole, CancellationToken>((role, _) => stored = role)
            .Returns(Task.CompletedTask);

        await _lifecycle.ApproveAsync(credential, 99, default);
        var second = await _lifecycle.ApproveAsync(credential, 99, default);

        Assert.True(second.IsFailure);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Once);
    }

    [Fact]
    public async Task Approve_RoleTakenBackEarlierFromCredential_ReactivatesIt()
    {
        var credential = Credential(CredentialStatus.UnderReview, id: 11);
        var role = new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = 4, IsActive = false };
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default)).ReturnsAsync(role);

        await _lifecycle.ApproveAsync(credential, 99, default);

        Assert.True(role.IsActive);
        Assert.Equal(11, role.GrantedByCredentialId);
        _roles.Verify(r => r.UpdateAsync(role, default), Times.Once);
    }

    [Fact]
    public async Task Approve_FromSubmitted_Fails()
    {
        var credential = Credential(CredentialStatus.Submitted);

        var result = await _lifecycle.ApproveAsync(credential, 99, default);

        Assert.True(result.IsFailure);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task Reject_FromUnderReview_StoresReasonAndGrantsNothing()
    {
        var credential = Credential(CredentialStatus.UnderReview);

        var result = await _lifecycle.RejectAsync(credential, "Número não encontrado", 99, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(CredentialStatus.Rejected, credential.Status);
        Assert.Equal("Número não encontrado", credential.RejectionReason);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task Reject_FromVerified_Fails()
    {
        var credential = Credential(CredentialStatus.Verified);

        var result = await _lifecycle.RejectAsync(credential, "x", 99, default);

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(CredentialStatus.Expired)]
    [InlineData(CredentialStatus.Suspended)]
    [InlineData(CredentialStatus.Revoked)]
    public async Task End_RoleGrantedByThisCredential_RemovesRole(CredentialStatus target)
    {
        var credential = Credential(CredentialStatus.Verified);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = 10 });

        var result = await _lifecycle.EndAsync(credential, target, Now, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, credential.Status);
        _roles.Verify(r => r.DeleteAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task End_RoleAssignedByHand_KeepsRole()
    {
        var credential = Credential(CredentialStatus.Verified);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = null });

        var result = await _lifecycle.EndAsync(credential, CredentialStatus.Revoked, Now, default);

        Assert.True(result.IsSuccess);
        _roles.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task End_RoleGrantedByAnotherCredential_KeepsRole()
    {
        var credential = Credential(CredentialStatus.Verified);
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default))
            .ReturnsAsync(new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = 55 });

        await _lifecycle.EndAsync(credential, CredentialStatus.Expired, Now, default);

        _roles.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task End_UserHasAnotherValidCredential_RoleStaysAndPassesToIt()
    {
        var credential = Credential(CredentialStatus.Verified, id: 10);
        var other = Credential(CredentialStatus.Verified, id: 12);
        var role = new UserPlatformRole { Id = 3, UserId = 7, Role = PlatformRoleType.Trainer, GrantedByCredentialId = 10 };
        _roles.Setup(r => r.GetByUserIdAndRoleAsync(7, PlatformRoleType.Trainer, default)).ReturnsAsync(role);
        _credentials.Setup(c => c.GetVerifiedAsync(7, "PersonalTrainer", Now, default)).ReturnsAsync(other);

        await _lifecycle.EndAsync(credential, CredentialStatus.Expired, Now, default);

        Assert.Equal(12, role.GrantedByCredentialId);
        _roles.Verify(r => r.UpdateAsync(role, default), Times.Once);
        _roles.Verify(r => r.DeleteAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Theory]
    [InlineData(CredentialStatus.UnderReview)]
    [InlineData(CredentialStatus.Rejected)]
    public async Task End_FromStatusOtherThanVerified_Fails(CredentialStatus from)
    {
        var credential = Credential(from);

        var result = await _lifecycle.EndAsync(credential, CredentialStatus.Expired, Now, default);

        Assert.True(result.IsFailure);
        Assert.Equal(from, credential.Status);
    }

    [Fact]
    public async Task End_TargetThatDoesNotWithdrawAccess_Fails()
    {
        var credential = Credential(CredentialStatus.UnderReview);

        var result = await _lifecycle.EndAsync(credential, CredentialStatus.Verified, Now, default);

        Assert.True(result.IsFailure);
    }
}
