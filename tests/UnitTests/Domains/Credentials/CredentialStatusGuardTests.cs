using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.StateMachine;

namespace UnitTests.Domains.Credentials;

public class CredentialStatusGuardTests
{
    [Theory]
    [InlineData(CredentialStatus.Draft, CredentialStatus.Submitted)]
    [InlineData(CredentialStatus.Submitted, CredentialStatus.UnderReview)]
    [InlineData(CredentialStatus.UnderReview, CredentialStatus.Verified)]
    [InlineData(CredentialStatus.UnderReview, CredentialStatus.Rejected)]
    [InlineData(CredentialStatus.Verified, CredentialStatus.Expired)]
    [InlineData(CredentialStatus.Verified, CredentialStatus.Suspended)]
    [InlineData(CredentialStatus.Verified, CredentialStatus.Revoked)]
    public void IsValidTransition_AllowedPair_ReturnsTrue(CredentialStatus from, CredentialStatus to)
    {
        Assert.True(CredentialStatusGuard.IsValidTransition(from, to));
    }

    [Theory]
    [InlineData(CredentialStatus.Draft, CredentialStatus.Verified)]
    [InlineData(CredentialStatus.Rejected, CredentialStatus.Submitted)]
    [InlineData(CredentialStatus.Verified, CredentialStatus.Draft)]
    [InlineData(CredentialStatus.Expired, CredentialStatus.Verified)]
    public void IsValidTransition_DisallowedPair_ReturnsFalse(CredentialStatus from, CredentialStatus to)
    {
        Assert.False(CredentialStatusGuard.IsValidTransition(from, to));
    }
}
