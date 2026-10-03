using FluentValidation;
using ShapeUp.Features.Credentials.Infrastructure.Verification;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.Credentials.Shared.Verification;
using ShapeUp.Features.Credentials.SubmitCredential;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.Credentials;

public class SubmitCredentialHandlerTests
{
    private readonly Mock<IProfessionalCredentialRepository> _credentials = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly CredentialLifecycle _lifecycle;

    public SubmitCredentialHandlerTests()
    {
        _credentials.Setup(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default))
            .Callback<ProfessionalCredential, CancellationToken>((c, _) => c.Id = 1)
            .Returns(Task.CompletedTask);
        _lifecycle = new CredentialLifecycle(_credentials.Object, new ProfessionalRoleGranter(_roles.Object, _credentials.Object));
    }

    private SubmitCredentialHandler Handler(params ICredentialVerifier[] verifiers) =>
        new(_credentials.Object, _lifecycle, verifiers, new SubmitCredentialValidator());

    private static SubmitCredentialCommand Valid(
        string profession = "PersonalTrainer",
        string authority = "CREF") =>
        new(profession, "123456-G/SP", authority, "SP", "BR");

    [Fact]
    public async Task HandleAsync_ManualReviewVerifier_CreatesSubmittedThenUnderReview()
    {
        var result = await Handler(new ManualReviewCredentialVerifier()).HandleAsync(Valid(), 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("UnderReview", result.Value!.Status);
        Assert.Equal(7, result.Value.UserId);
        Assert.NotNull(result.Value.SubmittedAt);
        _credentials.Verify(c => c.AddAsync(It.Is<ProfessionalCredential>(p => p.Status == CredentialStatus.Submitted || p.Status == CredentialStatus.UnderReview), default), Times.Once);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoVerifierForAuthority_FallsBackToManualReview()
    {
        var result = await Handler().HandleAsync(Valid(), 7, default);

        Assert.Equal("UnderReview", result.Value!.Status);
    }

    [Fact]
    public async Task HandleAsync_AutomaticVerifierConfirms_VerifiesAndGrantsRole()
    {
        var verifier = new Mock<ICredentialVerifier>();
        verifier.Setup(v => v.Supports("CRN")).Returns(true);
        verifier.Setup(v => v.VerifyAsync(It.IsAny<ProfessionalCredential>(), default))
            .ReturnsAsync(new CredentialVerificationResult(CredentialVerificationOutcome.Verified));

        var result = await Handler(verifier.Object).HandleAsync(Valid("Nutritionist", "CRN"), 7, default);

        Assert.Equal("Verified", result.Value!.Status);
        _roles.Verify(r => r.AddAsync(It.Is<UserPlatformRole>(x => x.GrantedByCredentialId == 1), default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AutomaticVerifierRejects_StoresReason()
    {
        var verifier = new Mock<ICredentialVerifier>();
        verifier.Setup(v => v.Supports("CREF")).Returns(true);
        verifier.Setup(v => v.VerifyAsync(It.IsAny<ProfessionalCredential>(), default))
            .ReturnsAsync(new CredentialVerificationResult(CredentialVerificationOutcome.Rejected, "Nome não confere"));

        var result = await Handler(verifier.Object).HandleAsync(Valid(), 7, default);

        Assert.Equal("Rejected", result.Value!.Status);
        Assert.Equal("Nome não confere", result.Value.RejectionReason);
        _roles.Verify(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AlreadyHasOpenCredentialForProfession_ReturnsConflict()
    {
        _credentials.Setup(c => c.HasOpenOrVerifiedAsync(7, "PersonalTrainer", default)).ReturnsAsync(true);

        var result = await Handler().HandleAsync(Valid(), 7, default);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
        _credentials.Verify(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    [Theory]
    [InlineData("Physiotherapist", "CREF", "SP", "BR")]
    [InlineData("PersonalTrainer", "CRN", "SP", "BR")]
    [InlineData("Nutritionist", "CREF", "SP", "BR")]
    [InlineData("PersonalTrainer", "CREF", "XX", "BR")]
    [InlineData("PersonalTrainer", "CREF", "sp", "BR")]
    [InlineData("PersonalTrainer", "CREF", "SP", "US")]
    public async Task HandleAsync_InvalidCombination_ReturnsValidationError(string profession, string authority, string region, string country)
    {
        var result = await Handler().HandleAsync(new SubmitCredentialCommand(profession, "123", authority, region, country), 7, default);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        _credentials.Verify(c => c.AddAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_EmptyNumber_ReturnsValidationError(string number)
    {
        var result = await Handler().HandleAsync(new SubmitCredentialCommand("PersonalTrainer", number, "CREF", "SP", "BR"), 7, default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        Assert.True(new SubmitCredentialValidator().Validate(Valid()).IsValid);
        Assert.True(new SubmitCredentialValidator().Validate(Valid("Nutritionist", "CRN")).IsValid);
    }
}
