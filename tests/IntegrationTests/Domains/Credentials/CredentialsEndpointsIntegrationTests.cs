namespace IntegrationTests.Domains.Credentials;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class CredentialsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task Submit_ShouldGoToReviewAndNotGrantCapabilityUntilApproved()
    {
        var professional = await SeedUserAsync();
        Authorize(professional.Token);

        var submit = await SubmitAsync("Nutritionist", "CRN", "SP");

        Assert.Equal(HttpStatusCode.Created, submit.StatusCode);
        var created = await submit.Content.ReadFromJsonAsync<CredentialPayload>();
        Assert.NotNull(created);
        Assert.Equal("UnderReview", created!.Status);

        var mine = await _client.GetFromJsonAsync<CredentialPayload[]>("/api/credentials/me");
        Assert.Contains(mine!, c => c.Id == created.Id);

        var capabilities = await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me");
        Assert.False(capabilities!.Nutrition);
    }

    [Fact]
    public async Task Approve_ShouldGrantNutritionCapability()
    {
        var professional = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);

        Authorize(professional.Token);
        var created = await (await SubmitAsync("Nutritionist", "CRN", "RJ")).Content.ReadFromJsonAsync<CredentialPayload>();

        Authorize(admin.Token);
        var queue = await _client.GetFromJsonAsync<CredentialPayload[]>("/api/credentials/under-review");
        Assert.Contains(queue!, c => c.Id == created!.Id);

        var approve = await _client.PostAsync($"/api/credentials/{created!.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var approved = await approve.Content.ReadFromJsonAsync<CredentialPayload>();
        Assert.Equal("Verified", approved!.Status);

        Authorize(professional.Token);
        var capabilities = await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me");
        Assert.True(capabilities!.Nutrition);
        Assert.False(capabilities.Training);
    }

    [Fact]
    public async Task Reject_ShouldKeepCapabilityOffAndStoreReason()
    {
        var professional = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);

        Authorize(professional.Token);
        var created = await (await SubmitAsync("PersonalTrainer", "CREF", "MG")).Content.ReadFromJsonAsync<CredentialPayload>();

        Authorize(admin.Token);
        var reject = await _client.PostAsJsonAsync($"/api/credentials/{created!.Id}/reject", new { reason = "Numero nao confere" });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        var rejected = await reject.Content.ReadFromJsonAsync<CredentialPayload>();
        Assert.Equal("Rejected", rejected!.Status);
        Assert.Equal("Numero nao confere", rejected.RejectionReason);

        Authorize(professional.Token);
        var capabilities = await _client.GetFromJsonAsync<CapabilitiesPayload>("/api/professional-capabilities/me");
        Assert.False(capabilities!.Training);
    }

    [Fact]
    public async Task ReviewEndpoints_WithoutAdminRole_ShouldReturnForbidden()
    {
        var user = await SeedUserAsync();
        Authorize(user.Token);

        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/credentials/under-review")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsync("/api/credentials/1/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync("/api/credentials/1/reject", new { reason = "x" })).StatusCode);
    }

    [Theory]
    [InlineData("Nutritionist", "CREF", "SP", "BR")]
    [InlineData("PersonalTrainer", "CRN", "SP", "BR")]
    [InlineData("Nutritionist", "CRN", "XX", "BR")]
    [InlineData("Nutritionist", "CRN", "SP", "US")]
    public async Task Submit_WithInvalidData_ShouldReturnBadRequest(string profession, string authority, string region, string country)
    {
        var user = await SeedUserAsync();
        Authorize(user.Token);

        var response = await _client.PostAsJsonAsync("/api/credentials", new
        {
            professionType = profession,
            credentialNumber = "12345",
            issuingAuthority = authority,
            issuingRegion = region,
            country
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_TwiceForTheSameProfession_ShouldReturnConflict()
    {
        var user = await SeedUserAsync();
        Authorize(user.Token);

        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync("Nutritionist", "CRN", "SP")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await SubmitAsync("Nutritionist", "CRN", "SP")).StatusCode);
    }

    private Task<HttpResponseMessage> SubmitAsync(string profession, string authority, string region) =>
        _client.PostAsJsonAsync("/api/credentials", new
        {
            professionType = profession,
            credentialNumber = $"{Random.Shared.Next(10000, 99999)}",
            issuingAuthority = authority,
            issuingRegion = region,
            country = "BR"
        });

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<TestUser> SeedUserAsync(bool asAdmin = false)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return new TestUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record TestUser(int UserId, string Token);
    private sealed record CapabilitiesPayload(bool Training, bool Nutrition);
    private sealed record CredentialPayload(int Id, int UserId, string ProfessionType, string Status, string? RejectionReason);
}
