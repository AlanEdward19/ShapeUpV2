namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

[Collection("SQL Server Write Operations")]
public sealed class FoodModerationEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private const string RejectionTemplateId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private TestEmailNotificationSender _emailSender = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        _emailSender = _factory.Services.GetRequiredService<TestEmailNotificationSender>();
        _emailSender.Clear();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPending_WhenNonAdmin_ReturnsForbidden()
    {
        var user = await SeedAuthorizedUserAsync(asAdmin: false);
        Authorize(user.Token);

        var response = await _client.GetAsync("/api/nutrition/food-moderation/pending");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Decide_WhenNonAdmin_ReturnsForbidden()
    {
        var user = await SeedAuthorizedUserAsync(asAdmin: false);
        Authorize(user.Token);

        var response = await _client.PostAsJsonAsync(
            "/api/nutrition/food-moderation/request-id/decide",
            new { decision = "Approved" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPending_WhenAdmin_ReturnsPublicAndProposedDiff()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, _) = await CreateFoodWithOverrideAsync(editor, publicKcal: 100, proposedKcal: 140);

        Authorize(admin.Token);
        var response = await _client.GetAsync("/api/nutrition/food-moderation/pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PendingModerationsPayload>();
        Assert.NotNull(payload);
        var item = Assert.Single(payload!.Items, x => x.FoodId == foodId);
        Assert.Equal(100, item.PublicMacros.Kcal);
        Assert.Equal(140, item.ProposedMacros.Kcal);
        Assert.Equal(editor.UserId, item.RequestedByUserId);
    }

    [Fact]
    public async Task Decide_Approve_UnifiesPublicFoodAndClearsAuthorOverride()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, foodName) = await CreateFoodWithOverrideAsync(editor, publicKcal: 90, proposedKcal: 120);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        Authorize(admin.Token);
        var decide = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Approved" });
        Assert.Equal(HttpStatusCode.OK, decide.StatusCode);

        var pending = await _client.GetAsync("/api/nutrition/food-moderation/pending");
        var pendingPayload = await pending.Content.ReadFromJsonAsync<PendingModerationsPayload>();
        Assert.DoesNotContain(pendingPayload!.Items, x => x.RequestId == requestId);

        Authorize(editor.Token);
        var search = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(foodName)}");
        var searchPayload = await search.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        var food = searchPayload!.Items.Single(x => x.Id == foodId);
        Assert.False(food.IsPersonalOverride);
        Assert.Equal(120, food.MacrosPer100.Kcal);
    }

    [Fact]
    public async Task Decide_WhenAlreadyDecided_ReturnsConflict()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, _) = await CreateFoodWithOverrideAsync(editor, publicKcal: 80, proposedKcal: 95);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        Authorize(admin.Token);
        var first = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var error = await second.Content.ReadFromJsonAsync<ErrorPayload>();
        Assert.NotNull(error);
        Assert.Equal("conflict", error!.Code);
    }

    [Fact]
    public async Task Decide_Reject_WhenEmailFlagEnabled_SendsEmailToAuthor()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, _) = await CreateFoodWithOverrideAsync(editor, publicKcal: 75, proposedKcal: 88);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        _emailSender.Clear();
        Authorize(admin.Token);
        var decide = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.OK, decide.StatusCode);

        var sentMessage = Assert.Single(_emailSender.Snapshot());
        Assert.Equal(editor.Email, sentMessage.To);
        Assert.Equal(RejectionTemplateId, sentMessage.TemplateId);
        Assert.Equal("Food edit rejected", sentMessage.Subject);
    }

    [Fact]
    public async Task Decide_Reject_WhenEmailFlagDisabled_DoesNotSendEmailButRejectionSucceeds()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, foodName) = await CreateFoodWithOverrideAsync(editor, publicKcal: 65, proposedKcal: 77);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        Authorize(admin.Token);
        var disableFlag = await _client.PutAsJsonAsync(
            "/api/platform/feature-flags/notifications.email-enabled",
            new { enabled = false });
        Assert.Equal(HttpStatusCode.OK, disableFlag.StatusCode);

        _emailSender.Clear();
        var decide = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.OK, decide.StatusCode);
        Assert.Empty(_emailSender.Snapshot());

        Authorize(editor.Token);
        var search = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(foodName)}");
        var searchPayload = await search.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        var food = searchPayload!.Items.Single(x => x.Id == foodId);
        Assert.True(food.IsPersonalOverride);
        Assert.Equal(77, food.MacrosPer100.Kcal);
    }

    [Fact]
    public async Task Decide_Reject_AfterReEnablingEmailFlag_SendsEmailOnNextRejection()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);

        Authorize(admin.Token);
        await _client.PutAsJsonAsync(
            "/api/platform/feature-flags/notifications.email-enabled",
            new { enabled = true });

        var (foodId, _) = await CreateFoodWithOverrideAsync(editor, publicKcal: 55, proposedKcal: 66);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        _emailSender.Clear();
        var decide = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.OK, decide.StatusCode);

        var sentMessage = Assert.Single(_emailSender.Snapshot());
        Assert.Equal(editor.Email, sentMessage.To);
        Assert.Equal(RejectionTemplateId, sentMessage.TemplateId);
    }

    [Fact]
    public async Task Decide_Reject_KeepsOverrideAndPublicFoodUnchanged()
    {
        var editor = await SeedAuthorizedUserAsync(asAdmin: false);
        var admin = await SeedAuthorizedUserAsync(asAdmin: true);
        var (foodId, foodName) = await CreateFoodWithOverrideAsync(editor, publicKcal: 70, proposedKcal: 99);
        var requestId = await GetPendingRequestIdAsync(admin.Token, foodId);

        Authorize(admin.Token);
        var decide = await _client.PostAsJsonAsync(
            $"/api/nutrition/food-moderation/{requestId}/decide",
            new { decision = "Rejected" });
        Assert.Equal(HttpStatusCode.OK, decide.StatusCode);

        Authorize(editor.Token);
        var search = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(foodName)}");
        var searchPayload = await search.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        var food = searchPayload!.Items.Single(x => x.Id == foodId);
        Assert.True(food.IsPersonalOverride);
        Assert.Equal(99, food.MacrosPer100.Kcal);

        var otherUser = await SeedAuthorizedUserAsync(asAdmin: false);
        Authorize(otherUser.Token);
        var otherSearch = await _client.GetAsync($"/api/nutrition/foods?query={Uri.EscapeDataString(foodName)}");
        var otherPayload = await otherSearch.Content.ReadFromJsonAsync<SearchFoodsPayload>();
        var otherView = otherPayload!.Items.Single(x => x.Id == foodId);
        Assert.False(otherView.IsPersonalOverride);
        Assert.Equal(70, otherView.MacrosPer100.Kcal);
    }

    private async Task<(string FoodId, string FoodName)> CreateFoodWithOverrideAsync(AuthorizedUser editor, int publicKcal, int proposedKcal)
    {
        Authorize(editor.Token);
        var uniqueName = $"ModerationFood-{Guid.NewGuid():N}";
        var create = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = uniqueName,
            macrosPer100 = new { kcal = publicKcal, proteinG = 10, carbG = 20, fatG = 5 }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var food = await create.Content.ReadFromJsonAsync<FoodPayload>();
        Assert.NotNull(food);

        var overrideResponse = await _client.PostAsJsonAsync(
            $"/api/nutrition/foods/{food!.Id}/override",
            new { macrosPer100 = new { kcal = proposedKcal, proteinG = 12, carbG = 22, fatG = 6 } });
        Assert.Equal(HttpStatusCode.OK, overrideResponse.StatusCode);

        return (food.Id, uniqueName);
    }

    private async Task<string> GetPendingRequestIdAsync(string adminToken, string foodId)
    {
        Authorize(adminToken);
        var pending = await _client.GetAsync("/api/nutrition/food-moderation/pending");
        var payload = await pending.Content.ReadFromJsonAsync<PendingModerationsPayload>();
        return payload!.Items.Single(x => x.FoodId == foodId).RequestId;
    }

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync(bool asAdmin)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return new AuthorizedUser(user.Id, user.Email, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record AuthorizedUser(int UserId, string Email, string Token);
    private sealed record ErrorPayload(string Code, string Message);
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record FoodPayload(string Id, string Name, MacroPayload MacrosPer100, bool IsPersonalOverride);
    private sealed record SearchFoodsPayload(FoodPayload[] Items, string? NextCursor);
    private sealed record PendingModerationItem(
        string RequestId,
        string FoodId,
        int RequestedByUserId,
        MacroPayload PublicMacros,
        MacroPayload ProposedMacros);
    private sealed record PendingModerationsPayload(PendingModerationItem[] Items, string? NextCursor);
}
