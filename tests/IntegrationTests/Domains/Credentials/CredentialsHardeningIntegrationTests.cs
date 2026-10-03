namespace IntegrationTests.Domains.Credentials;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.Credentials.ExpireCredentials;
using ShapeUp.Features.Credentials.Infrastructure.Repositories;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Credentials.Shared.Errors;
using ShapeUp.Features.GymManagement.Shared.Entities;

[Collection("SQL Server Write Operations")]
public sealed class CredentialsHardeningIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UnderReview_ShouldShowRequesterAndPaginate()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var first = await SeedUserAsync(displayName: "Ana Souza");
        var second = await SeedUserAsync();
        var firstCredential = await SubmitOkAsync(first, "Nutritionist", "CRN", "SP", NewNumber());
        var secondCredential = await SubmitOkAsync(second, "Nutritionist", "CRN", "SP", NewNumber());

        Authorize(admin);
        var page = await _client.GetFromJsonAsync<QueuePayload>("/api/credentials/under-review?pageSize=1");

        var item = Assert.Single(page!.Items);
        Assert.NotNull(page.NextCursor);

        // Walk the queue until both submissions were seen; the requester data must be present on each.
        var seen = new List<CredentialPayload> { item };
        var cursor = page.NextCursor;
        while (cursor is not null)
        {
            var next = await _client.GetFromJsonAsync<QueuePayload>($"/api/credentials/under-review?pageSize=50&cursor={Uri.EscapeDataString(cursor)}");
            seen.AddRange(next!.Items);
            cursor = next.NextCursor;
        }

        var mine = seen.Single(c => c.Id == firstCredential);
        Assert.Equal("Ana Souza", mine.RequesterName);
        Assert.Equal(first.Email, mine.RequesterEmail);
        Assert.Contains(seen, c => c.Id == secondCredential && c.RequesterEmail == second.Email);

        var approve = await _client.PostAsync($"/api/credentials/{firstCredential}/approve", null);
        var approved = await approve.Content.ReadFromJsonAsync<CredentialPayload>();
        Assert.Equal(first.Email, approved!.RequesterEmail);
        Assert.Equal("Ana Souza", approved.RequesterName);
    }

    [Fact]
    public async Task UnderReview_WithInvalidCursor_ShouldReturnBadRequest()
    {
        Authorize(await SeedUserAsync(asAdmin: true));

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/credentials/under-review?cursor=zzz")).StatusCode);
    }

    [Fact]
    public async Task Submit_SameRegistrationByTwoUsers_ShouldReturnConflict()
    {
        var number = NewNumber();
        var first = await SeedUserAsync();
        var second = await SeedUserAsync();
        await SubmitOkAsync(first, "PersonalTrainer", "CREF", "SP", number);

        Authorize(second);
        var response = await SubmitAsync("PersonalTrainer", "CREF", "SP", number.ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Submit_SameRegistrationAfterRejection_ShouldBeAccepted()
    {
        var number = NewNumber();
        var admin = await SeedUserAsync(asAdmin: true);
        var first = await SeedUserAsync();
        var second = await SeedUserAsync();
        var id = await SubmitOkAsync(first, "PersonalTrainer", "CREF", "RJ", number);
        Authorize(admin);
        await _client.PostAsJsonAsync($"/api/credentials/{id}/reject", new { reason = "inativo" });

        Authorize(second);
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync("PersonalTrainer", "CREF", "RJ", number)).StatusCode);
    }

    [Fact]
    public async Task Submit_ConcurrentRequestsFromTheSameUser_ShouldCreateOneAndConflictTheRest()
    {
        var user = await SeedUserAsync();
        Authorize(user);

        var responses = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => SubmitAsync("Nutritionist", "CRN", "MG", NewNumber())));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        await using var context = fixture.CreateCredentialsDbContext();
        Assert.Equal(1, await context.ProfessionalCredentials.CountAsync(c => c.UserId == user.UserId));
    }

    [Fact]
    public async Task Submit_ConcurrentRequestsWithTheSameRegistration_ShouldCreateOneAndConflictTheRest()
    {
        var number = NewNumber();
        var users = new[] { await SeedUserAsync(), await SeedUserAsync(), await SeedUserAsync(), await SeedUserAsync() };

        var responses = await Task.WhenAll(users.Select(async u =>
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.Token);
            return await PostSubmitAsync(client, "PersonalTrainer", "CREF", "PR", number);
        }));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task Approve_ConcurrentDecisions_ShouldHaveOneWinnerAndOneRole()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var professional = await SeedUserAsync();
        var id = await SubmitOkAsync(professional, "PersonalTrainer", "CREF", "SC", NewNumber());
        Authorize(admin);

        var responses = await Task.WhenAll(
            _client.PostAsync($"/api/credentials/{id}/approve", null),
            _client.PostAsync($"/api/credentials/{id}/approve", null),
            _client.PostAsync($"/api/credentials/{id}/approve", null),
            _client.PostAsJsonAsync($"/api/credentials/{id}/reject", new { reason = "x" }));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        await using var credentials = fixture.CreateCredentialsDbContext();
        var final = await credentials.ProfessionalCredentials.SingleAsync(c => c.Id == id);
        await using var gym = fixture.CreateGymManagementDbContext();
        var roles = await gym.UserPlatformRoles.Where(r => r.UserId == professional.UserId && r.Role == PlatformRoleType.Trainer).ToListAsync();

        // Whatever won, the role matches the final status.
        Assert.Equal(final.Status == CredentialStatus.Verified ? 1 : 0, roles.Count);
    }

    [Fact]
    public async Task Repository_Update_WithStaleRowVersion_ShouldThrowConcurrentChange()
    {
        var professional = await SeedUserAsync();
        var id = await SubmitOkAsync(professional, "PersonalTrainer", "CREF", "BA", NewNumber());

        await using var contextA = fixture.CreateCredentialsDbContext();
        await using var contextB = fixture.CreateCredentialsDbContext();
        var repoA = new ProfessionalCredentialRepository(contextA);
        var repoB = new ProfessionalCredentialRepository(contextB);
        var a = (await repoA.GetByIdAsync(id, default))!;
        var b = (await repoB.GetByIdAsync(id, default))!;

        a.Status = CredentialStatus.Verified;
        await repoA.UpdateAsync(a, default);
        b.Status = CredentialStatus.Rejected;
        var ex = await Assert.ThrowsAsync<CredentialConflictException>(() => repoB.UpdateAsync(b, default));

        Assert.Equal(CredentialConflictKind.ConcurrentChange, ex.Kind);
    }

    [Fact]
    public async Task Repository_Add_DuplicateOpenRegistration_ShouldThrowRegistrationInUse()
    {
        var number = NewNumber();
        var first = await SeedUserAsync();
        var second = await SeedUserAsync();

        await using var contextA = fixture.CreateCredentialsDbContext();
        await using var contextB = fixture.CreateCredentialsDbContext();
        await new ProfessionalCredentialRepository(contextA).AddAsync(Credential(first.UserId, number), default);

        var ex = await Assert.ThrowsAsync<CredentialConflictException>(
            () => new ProfessionalCredentialRepository(contextB).AddAsync(Credential(second.UserId, number), default));

        Assert.Equal(CredentialConflictKind.RegistrationInUse, ex.Kind);
    }

    [Fact]
    public async Task Approve_ShouldDefaultExpiryToTwelveMonthsAndHonourExplicitDate()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var first = await SeedUserAsync();
        var second = await SeedUserAsync();
        var defaultId = await SubmitOkAsync(first, "Nutritionist", "CRN", "SP", NewNumber());
        var explicitId = await SubmitOkAsync(second, "Nutritionist", "CRN", "SP", NewNumber());
        Authorize(admin);

        var byDefault = await (await _client.PostAsync($"/api/credentials/{defaultId}/approve", null)).Content.ReadFromJsonAsync<CredentialPayload>();
        var wanted = DateTime.UtcNow.AddMonths(6);
        var explicitResponse = await _client.PostAsJsonAsync($"/api/credentials/{explicitId}/approve", new { expiresAt = wanted });
        var explicitPayload = await explicitResponse.Content.ReadFromJsonAsync<CredentialPayload>();

        Assert.InRange(byDefault!.ExpiresAt!.Value, DateTime.UtcNow.AddMonths(12).AddMinutes(-5), DateTime.UtcNow.AddMonths(12).AddMinutes(5));
        Assert.InRange(explicitPayload!.ExpiresAt!.Value, wanted.AddSeconds(-5), wanted.AddSeconds(5));
    }

    [Fact]
    public async Task Approve_WithPastExpiry_ShouldReturnBadRequest()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var id = await SubmitOkAsync(await SeedUserAsync(), "Nutritionist", "CRN", "SP", NewNumber());
        Authorize(admin);

        var response = await _client.PostAsJsonAsync($"/api/credentials/{id}/approve", new { expiresAt = DateTime.UtcNow.AddDays(-1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("suspend", "Suspended")]
    [InlineData("revoke", "Revoked")]
    public async Task SuspendAndRevoke_ShouldEndCredentialAndWithdrawCapability(string action, string expectedStatus)
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var professional = await SeedUserAsync();
        var id = await SubmitOkAsync(professional, "Nutritionist", "CRN", "SP", NewNumber());
        Authorize(admin);
        await _client.PostAsync($"/api/credentials/{id}/approve", null);
        Authorize(professional);
        Assert.True((await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me"))!.Nutrition);

        Authorize(admin);
        var response = await _client.PostAsJsonAsync($"/api/credentials/{id}/{action}", new { reason = "Denuncia no conselho" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CredentialPayload>();
        Assert.Equal(expectedStatus, payload!.Status);
        Assert.Equal("Denuncia no conselho", payload.EndReason);

        Authorize(professional);
        Assert.False((await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me"))!.Nutrition);

        Authorize(admin);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync($"/api/credentials/{id}/revoke", new { reason = "de novo" })).StatusCode);
    }

    [Fact]
    public async Task SuspendAndRevoke_WithoutAdminOrReason_ShouldBeRejected()
    {
        var user = await SeedUserAsync();
        Authorize(user);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/credentials/1/suspend", new { reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/credentials/1/revoke", new { reason = "x" })).StatusCode);

        Authorize(await SeedUserAsync(asAdmin: true));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/credentials/1/revoke", new { reason = "" })).StatusCode);
    }

    [Fact]
    public async Task ExpiryJob_ShouldExpireOverdueCredentialAndWithdrawCapability()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var professional = await SeedUserAsync();
        var id = await SubmitOkAsync(professional, "PersonalTrainer", "CREF", "SP", NewNumber());
        Authorize(admin);
        await _client.PostAsync($"/api/credentials/{id}/approve", null);
        Authorize(professional);
        Assert.True((await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me"))!.Training);

        await using (var context = fixture.CreateCredentialsDbContext())
        {
            await context.ProfessionalCredentials.Where(c => c.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var expired = await scope.ServiceProvider.GetRequiredService<ExpireCredentialsHandler>()
                .HandleAsync(DateTime.UtcNow, CancellationToken.None);
            Assert.True(expired >= 1);
        }

        await using (var context = fixture.CreateCredentialsDbContext())
            Assert.Equal(CredentialStatus.Expired, (await context.ProfessionalCredentials.SingleAsync(c => c.Id == id)).Status);
        await using (var gym = fixture.CreateGymManagementDbContext())
            Assert.False(await gym.UserPlatformRoles.AnyAsync(r => r.UserId == professional.UserId && r.Role == PlatformRoleType.Trainer));
        Assert.False((await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me"))!.Training);

        // Reverification: an expired credential does not block a new submission.
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync("PersonalTrainer", "CREF", "SP", NewNumber())).StatusCode);
    }

    [Fact]
    public async Task AdminAssigningRoleGrantedByCredential_ShouldAdoptItAndSurviveRevocation()
    {
        var admin = await SeedUserAsync(asAdmin: true);
        var professional = await SeedUserAsync();
        var id = await SubmitOkAsync(professional, "PersonalTrainer", "CREF", "SP", NewNumber());
        Authorize(admin);
        await _client.PostAsync($"/api/credentials/{id}/approve", null);

        var assign = await _client.PostAsJsonAsync("/api/gym-management/user-roles", new { userId = professional.UserId, role = "Trainer" });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);

        await _client.PostAsJsonAsync($"/api/credentials/{id}/revoke", new { reason = "x" });

        await using var gym = fixture.CreateGymManagementDbContext();
        var role = await gym.UserPlatformRoles.SingleAsync(r => r.UserId == professional.UserId && r.Role == PlatformRoleType.Trainer);
        Assert.Null(role.GrantedByCredentialId);

        // A second manual assignment is still a conflict.
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync("/api/gym-management/user-roles", new { userId = professional.UserId, role = "Trainer" })).StatusCode);
    }

    private static string NewNumber() => $"{Random.Shared.Next(100000, 999999)}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static ProfessionalCredential Credential(int userId, string number) => new()
    {
        UserId = userId, ProfessionType = "PersonalTrainer", CredentialNumber = number, IssuingAuthority = "CREF",
        IssuingRegion = "SP", Country = "BR", Status = CredentialStatus.UnderReview, SubmittedAt = DateTime.UtcNow
    };

    private Task<HttpResponseMessage> SubmitAsync(string profession, string authority, string region, string number) =>
        PostSubmitAsync(_client, profession, authority, region, number);

    private static Task<HttpResponseMessage> PostSubmitAsync(HttpClient client, string profession, string authority, string region, string number) =>
        client.PostAsJsonAsync("/api/credentials", new
        {
            professionType = profession,
            credentialNumber = number,
            issuingAuthority = authority,
            issuingRegion = region,
            country = "BR"
        });

    private async Task<int> SubmitOkAsync(TestUser user, string profession, string authority, string region, string number)
    {
        Authorize(user);
        var response = await SubmitAsync(profession, authority, region, number);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CredentialPayload>())!.Id;
    }

    private void Authorize(TestUser user) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

    private async Task<TestUser> SeedUserAsync(bool asAdmin = false, string? displayName = null)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        if (displayName is not null)
        {
            user.DisplayName = displayName;
            await context.SaveChangesAsync();
        }

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return new TestUser(user.Id, user.Email, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record TestUser(int UserId, string Email, string Token);
    private sealed record CapabilitiesPayload(bool Training, bool Nutrition);
    private sealed record QueuePayload(CredentialPayload[] Items, string? NextCursor);
    private sealed record CredentialPayload(
        int Id, int UserId, string ProfessionType, string Status, DateTime? ExpiresAt, string? EndReason,
        string? RequesterName, string? RequesterEmail);
}
