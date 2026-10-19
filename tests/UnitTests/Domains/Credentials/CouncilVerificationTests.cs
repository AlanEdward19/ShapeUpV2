using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Credentials.Infrastructure.Verification;
using ShapeUp.Features.Credentials.ReverifyCredentials;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Lifecycle;
using ShapeUp.Features.Credentials.Shared.Verification;
using ShapeUp.Features.GymManagement.Shared.Abstractions;

namespace UnitTests.Domains.Credentials;

/// <summary>Council answers are recorded fixtures: nothing here calls a real council.</summary>
public class CouncilVerificationTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private static IOptions<CouncilVerificationOptions> Options(bool enabled = true, bool rejectWhenNotFound = true, int retries = 1, int threshold = 5) =>
        Microsoft.Extensions.Options.Options.Create(new CouncilVerificationOptions
        {
            Crn = new CouncilEndpointOptions
            {
                Enabled = enabled, BaseUrl = "https://lookup.test/crn", ApiKey = "k", Retries = retries,
                BreakerThreshold = threshold, RejectWhenNotFound = rejectWhenNotFound, TimeoutSeconds = 2
            },
            Cref = new CouncilEndpointOptions { Enabled = false }
        });

    private static ProfessionalCredential Credential(string authority = "CRN", string region = "SP", CredentialStatus status = CredentialStatus.UnderReview) => new()
    {
        Id = 5, UserId = 7, ProfessionType = authority == "CRN" ? "Nutritionist" : "PersonalTrainer", CredentialNumber = "12345",
        IssuingAuthority = authority, IssuingRegion = region, Country = "BR", Status = status
    };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls;
        public HttpRequestMessage? Last;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (HttpCouncilRegistryClient Client, FakeHandler Handler, CouncilCircuitBreaker Breaker) Http(
        Func<HttpRequestMessage, HttpResponseMessage> respond, IOptions<CouncilVerificationOptions>? options = null)
    {
        var handler = new FakeHandler(respond);
        var breaker = new CouncilCircuitBreaker();
        return (new HttpCouncilRegistryClient(new HttpClient(handler), options ?? Options(), breaker), handler, breaker);
    }

    // Name matching ------------------------------------------------------------------------------

    [Theory]
    [InlineData("Maria Souza", "MARIA DA SILVA SOUZA", true)]
    [InlineData("José Antônio Lima", "Jose Antonio Lima", true)]
    [InlineData("Maria Souza", "Mariana Souza", false)]
    [InlineData("Maria", "Maria Souza", false)]
    [InlineData("Maria Souza", "Maria Santos", false)]
    [InlineData("Ana Paula Souza", "Ana Clara Souza", false)]
    [InlineData(null, "Maria Souza", false)]
    [InlineData("Maria Souza", "", false)]
    public void NameMatcher_StrongMatchRules(string? account, string? council, bool expected) =>
        Assert.Equal(expected, PersonNameMatcher.IsStrongMatch(account, council));

    // HTTP client --------------------------------------------------------------------------------

    [Fact]
    public async Task Client_Found_ParsesRecordedResponse()
    {
        var (client, handler, _) = Http(_ => Json("""{"found":true,"active":true,"name":"MARIA DA SILVA SOUZA","region":"SP"}"""));

        var result = await client.LookupAsync("CRN", "SP", "12345", default);

        Assert.Equal(CouncilLookupStatus.Found, result.Status);
        Assert.True(result.Active);
        Assert.Equal("MARIA DA SILVA SOUZA", result.Name);
        Assert.Contains("number=12345", handler.Last!.RequestUri!.Query);
        Assert.Contains("region=SP", handler.Last.RequestUri.Query);
        Assert.Equal("Bearer", handler.Last.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Client_404_IsNotFound() =>
        Assert.Equal(CouncilLookupStatus.NotFound, (await Http(_ => new HttpResponseMessage(HttpStatusCode.NotFound)).Client.LookupAsync("CRN", "SP", "1", default)).Status);

    [Fact]
    public async Task Client_FoundFalse_IsNotFound() =>
        Assert.Equal(CouncilLookupStatus.NotFound, (await Http(_ => Json("""{"found":false}""")).Client.LookupAsync("CRN", "SP", "1", default)).Status);

    [Fact]
    public async Task Client_ServerError_RetriesThenUnavailable()
    {
        var (client, handler, _) = Http(_ => new HttpResponseMessage(HttpStatusCode.BadGateway), Options(retries: 2));

        var result = await client.LookupAsync("CRN", "SP", "1", default);

        Assert.Equal(CouncilLookupStatus.Unavailable, result.Status);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task Client_ClientError_DoesNotRetry()
    {
        var (client, handler, _) = Http(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized), Options(retries: 3));

        Assert.Equal(CouncilLookupStatus.Unavailable, (await client.LookupAsync("CRN", "SP", "1", default)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Client_NetworkError_IsUnavailable()
    {
        var (client, _, _) = Http(_ => throw new HttpRequestException("dns"), Options(retries: 0));

        Assert.Equal(CouncilLookupStatus.Unavailable, (await client.LookupAsync("CRN", "SP", "1", default)).Status);
    }

    [Fact]
    public async Task Client_CircuitOpensAfterThreshold_AndStopsCalling()
    {
        var (client, handler, breaker) = Http(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError), Options(retries: 0, threshold: 2));

        await client.LookupAsync("CRN", "SP", "1", default);
        await client.LookupAsync("CRN", "SP", "1", default);
        Assert.True(breaker.IsOpen("CRN"));
        var calls = handler.Calls;

        var result = await client.LookupAsync("CRN", "SP", "1", default);

        Assert.Equal(CouncilLookupStatus.Unavailable, result.Status);
        Assert.Equal(calls, handler.Calls);
    }

    [Fact]
    public async Task Client_Disabled_NeverCallsCouncil()
    {
        var (client, handler, _) = Http(_ => Json("""{"found":true}"""), Options(enabled: false));

        Assert.Equal(CouncilLookupStatus.Unavailable, (await client.LookupAsync("CRN", "SP", "1", default)).Status);
        Assert.Equal(0, handler.Calls);
    }

    // Checker + verifier -------------------------------------------------------------------------

    private static (CouncilRegistryChecker Checker, Mock<ICredentialVerificationAuditRepository> Audit) Checker(
        CouncilLookupResult lookup, string? userName = "Maria Souza", IOptions<CouncilVerificationOptions>? options = null)
    {
        var client = new Mock<ICouncilRegistryClient>();
        client.Setup(c => c.LookupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync(lookup);
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(7, default)).ReturnsAsync(new User { Id = 7, FirebaseUid = "f", Email = "m@x.com", DisplayName = userName });
        var audit = new Mock<ICredentialVerificationAuditRepository>();
        return (new CouncilRegistryChecker(client.Object, users.Object, audit.Object, options ?? Options()), audit);
    }

    private static CouncilLookupResult Found(string name = "MARIA DA SILVA SOUZA", string? region = "SP", bool active = true) =>
        new(CouncilLookupStatus.Found, active, name, region);

    [Fact]
    public async Task Checker_ActiveAndNameMatches_IsVerified_AndAudited()
    {
        var (checker, audit) = Checker(Found());

        var result = await checker.CheckAsync(Credential(), default);

        Assert.Equal(CouncilCheckOutcome.Verified, result.Outcome);
        audit.Verify(a => a.AddAsync(It.Is<CredentialVerificationAudit>(x =>
            x.CredentialId == 5 && x.Source == "council:CRN" && x.Outcome == "Verified"), default), Times.Once);
    }

    [Fact]
    public async Task Checker_NameDoesNotMatch_IsInconclusive() =>
        Assert.Equal(CouncilCheckOutcome.Inconclusive, (await Checker(Found("OUTRA PESSOA SANTOS")).Checker.CheckAsync(Credential(), default)).Outcome);

    [Fact]
    public async Task Checker_OtherPersonsRegistration_NeverVerified() =>
        Assert.NotEqual(CouncilCheckOutcome.Verified, (await Checker(Found("JOAO PEREIRA"), "Maria Souza").Checker.CheckAsync(Credential(), default)).Outcome);

    [Fact]
    public async Task Checker_DifferentRegion_IsInconclusive() =>
        Assert.Equal(CouncilCheckOutcome.Inconclusive, (await Checker(Found(region: "RJ")).Checker.CheckAsync(Credential(), default)).Outcome);

    [Fact]
    public async Task Checker_Inactive_IsNotFound() =>
        Assert.Equal(CouncilCheckOutcome.NotFound, (await Checker(Found(active: false)).Checker.CheckAsync(Credential(), default)).Outcome);

    [Fact]
    public async Task Checker_CouncilUnavailable_IsInconclusive() =>
        Assert.Equal(CouncilCheckOutcome.Inconclusive, (await Checker(CouncilLookupResult.Unavailable()).Checker.CheckAsync(Credential(), default)).Outcome);

    [Fact]
    public async Task Checker_AuditFailure_DoesNotChangeDecision()
    {
        var (checker, audit) = Checker(Found());
        audit.Setup(a => a.AddAsync(It.IsAny<CredentialVerificationAudit>(), default)).ThrowsAsync(new Exception("db"));

        Assert.Equal(CouncilCheckOutcome.Verified, (await checker.CheckAsync(Credential(), default)).Outcome);
    }

    [Fact]
    public async Task Verifier_Verified_ApprovesAutomatically()
    {
        var (checker, _) = Checker(Found());
        var verifier = new CrnCredentialVerifier(checker, Options());

        Assert.True(verifier.Supports("CRN"));
        Assert.Equal(CredentialVerificationOutcome.Verified, (await verifier.VerifyAsync(Credential(), default)).Outcome);
    }

    [Fact]
    public async Task Verifier_NotFound_RejectsWithReason_WhenConfigured()
    {
        var (checker, _) = Checker(new CouncilLookupResult(CouncilLookupStatus.NotFound));
        var result = await new CrnCredentialVerifier(checker, Options()).VerifyAsync(Credential(), default);

        Assert.Equal(CredentialVerificationOutcome.Rejected, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Fact]
    public async Task Verifier_NotFound_GoesToReview_WhenRejectIsOff()
    {
        var options = Options(rejectWhenNotFound: false);
        var (checker, _) = Checker(new CouncilLookupResult(CouncilLookupStatus.NotFound), options: options);

        Assert.Equal(CredentialVerificationOutcome.UnderReview,
            (await new CrnCredentialVerifier(checker, options).VerifyAsync(Credential(), default)).Outcome);
    }

    [Fact]
    public async Task Verifier_Outage_KeepsManualReview()
    {
        var (checker, _) = Checker(CouncilLookupResult.Unavailable());

        Assert.Equal(CredentialVerificationOutcome.UnderReview,
            (await new CrnCredentialVerifier(checker, Options()).VerifyAsync(Credential(), default)).Outcome);
    }

    [Fact]
    public void Verifier_FlagOff_DoesNotSupport_AndOtherCouncilIgnored()
    {
        var off = Options(enabled: false);
        var (checker, _) = Checker(Found(), options: off);

        Assert.False(new CrnCredentialVerifier(checker, off).Supports("CRN"));
        Assert.False(new CrnCredentialVerifier(Checker(Found()).Checker, Options()).Supports("CREF"));
        Assert.False(new CrefCredentialVerifier(Checker(Found()).Checker, Options()).Supports("CREF"));
    }

    // Reverification -----------------------------------------------------------------------------

    private static ReverifyCredentialsHandler Reverify(Mock<IProfessionalCredentialRepository> repo, ICouncilRegistryChecker checker, Mock<ICredentialLifecycle> lifecycle) =>
        new(repo.Object, checker, lifecycle.Object, Options());

    private static Mock<IProfessionalCredentialRepository> RepoWith(params ProfessionalCredential[] due)
    {
        var repo = new Mock<IProfessionalCredentialRepository>();
        repo.Setup(r => r.GetVerifiedDueForCouncilCheckAsync("CRN", It.IsAny<DateTime>(), Now, 50, default)).ReturnsAsync(due);
        repo.Setup(r => r.GetVerifiedDueForCouncilCheckAsync("CREF", It.IsAny<DateTime>(), Now, 50, default)).ReturnsAsync([]);
        return repo;
    }

    private static Mock<ICouncilRegistryChecker> Stub(CouncilCheckOutcome outcome)
    {
        var checker = new Mock<ICouncilRegistryChecker>();
        checker.Setup(c => c.IsEnabled("CRN")).Returns(true);
        checker.Setup(c => c.CheckAsync(It.IsAny<ProfessionalCredential>(), default)).ReturnsAsync(new CouncilCheckResult(outcome, "x"));
        return checker;
    }

    [Fact]
    public async Task Reverify_RegistrationNoLongerConfirmed_SuspendsAndWithdrawsRole()
    {
        var credential = Credential(status: CredentialStatus.Verified);
        var lifecycle = new Mock<ICredentialLifecycle>();
        lifecycle.Setup(l => l.EndAsync(credential, CredentialStatus.Suspended, Now, default, ReverifyCredentialsHandler.SuspensionReason, null))
            .ReturnsAsync(ShapeUp.Shared.Results.Result.Success());

        var suspended = await Reverify(RepoWith(credential), Stub(CouncilCheckOutcome.NotFound).Object, lifecycle).HandleAsync(Now, default);

        Assert.Equal(1, suspended);
        lifecycle.VerifyAll();
    }

    [Fact]
    public async Task Reverify_StillValid_StampsCheckDateOnly()
    {
        var credential = Credential(status: CredentialStatus.Verified);
        var repo = RepoWith(credential);
        var lifecycle = new Mock<ICredentialLifecycle>();

        var suspended = await Reverify(repo, Stub(CouncilCheckOutcome.Verified).Object, lifecycle).HandleAsync(Now, default);

        Assert.Equal(0, suspended);
        Assert.Equal(Now, credential.LastCouncilCheckAt);
        repo.Verify(r => r.UpdateAsync(credential, default), Times.Once);
        lifecycle.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reverify_CouncilOutage_ChangesNothing()
    {
        var credential = Credential(status: CredentialStatus.Verified);
        var repo = RepoWith(credential);
        var lifecycle = new Mock<ICredentialLifecycle>();

        await Reverify(repo, Stub(CouncilCheckOutcome.Inconclusive).Object, lifecycle).HandleAsync(Now, default);

        Assert.Null(credential.LastCouncilCheckAt);
        repo.Verify(r => r.UpdateAsync(It.IsAny<ProfessionalCredential>(), default), Times.Never);
        lifecycle.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reverify_NoCouncilEnabled_DoesNothing()
    {
        var repo = new Mock<IProfessionalCredentialRepository>();
        var checker = new Mock<ICouncilRegistryChecker>();

        var suspended = await Reverify(repo, checker.Object, new Mock<ICredentialLifecycle>()).HandleAsync(Now, default);

        Assert.Equal(0, suspended);
        repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reverify_OneFailure_DoesNotStopTheBatch()
    {
        var bad = Credential(status: CredentialStatus.Verified);
        var good = Credential(status: CredentialStatus.Verified);
        good.Id = 6;
        var checker = new Mock<ICouncilRegistryChecker>();
        checker.Setup(c => c.IsEnabled("CRN")).Returns(true);
        checker.Setup(c => c.CheckAsync(bad, default)).ThrowsAsync(new Exception("x"));
        checker.Setup(c => c.CheckAsync(good, default)).ReturnsAsync(new CouncilCheckResult(CouncilCheckOutcome.Verified, "ok"));
        var repo = RepoWith(bad, good);

        await Reverify(repo, checker.Object, new Mock<ICredentialLifecycle>()).HandleAsync(Now, default);

        Assert.Equal(Now, good.LastCouncilCheckAt);
    }
}
