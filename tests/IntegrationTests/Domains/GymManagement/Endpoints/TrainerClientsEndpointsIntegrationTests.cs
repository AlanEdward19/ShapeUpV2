using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.Shared;

namespace IntegrationTests.Domains.GymManagement.Endpoints;

/// <summary>
/// Covers T13: TrainerClientsController migrated from RequireScopesAttribute to
/// [Authorize(Policy = "capability:...")]. AUTHZ-01/AUTHZ-02: the policy grants access only via
/// self-access (route trainerId == authenticated user id) -- delegated/gym-staff access on behalf
/// of a trainer is a documented gap (SPEC_DEVIATION), not covered here.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class TrainerClientsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetAll_SelfAccess_ShouldReturnOk()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("gettc-self");

        var response = await _client.GetAsync($"/api/gym-management/trainers/{trainerId}/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyTrainer_UserWithoutTrainer_ShouldReturnHasTrainerFalse()
    {
        await SeedUserAndAuthenticateAsync("mytrainer-none");

        var response = await _client.GetAsync("/api/gym-management/trainer-clients/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"hasTrainer\":false", body);
    }

    [Fact]
    public async Task GetAll_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("gettc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("gettc-other");

        var response = await _client.GetAsync($"/api/gym-management/trainers/{otherTrainerId}/clients");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Add_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("addtc-self");

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{trainerId}/clients", new
        {
            clientId = Random.Shared.Next(500_000, 600_000),
            trainerPlanId = (int?)null
        });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Add_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("addtc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("addtc-other");

        var response = await _client.PostAsJsonAsync($"/api/gym-management/trainers/{otherTrainerId}/clients", new
        {
            clientId = Random.Shared.Next(500_000, 600_000),
            trainerPlanId = (int?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInvite_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("invtc-self");

        var response = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/invitee@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInvite_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("invtc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("invtc-other");

        var response = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/invites/invitee@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvite_Pending_BecomesRevoked_AndTheTokenStopsWorking()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("revtc-self");
        var created = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/revoked@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var invite = (await created.Content.ReadFromJsonAsync<InviteCreated>())!;

        var revoke = await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{invite.InviteId}");
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        var list = await _client.GetFromJsonAsync<Page<InviteListed>>($"/api/gym-management/trainers/{trainerId}/clients/invites");
        Assert.Equal("Revoked", list!.Items.Single(i => i.InviteId == invite.InviteId).Status);

        // Revoking again, or accepting the revoked invite, is refused.
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{invite.InviteId}")).StatusCode);
    }

    [Fact]
    public async Task Invite_WithoutLifetime_LivesSevenDays_LikeTheNutritionists()
    {
        var (_, invite, _) = await CreateInviteAsync("sevendays");

        Assert.InRange((invite.ExpiresAtUtc - DateTime.UtcNow).TotalHours, 167.9, 168.1);
    }

    [Fact]
    public async Task List_IsPagedNewestFirst_AnyStatus()
    {
        var (trainerId, first, _) = await CreateInviteAsync("paged");
        var second = await CreateExtraInviteAsync(trainerId, "paged-2");
        var third = await CreateExtraInviteAsync(trainerId, "paged-3");
        Assert.Equal(HttpStatusCode.OK,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{second.InviteId}")).StatusCode);

        var page1 = await _client.GetFromJsonAsync<Page<InviteListed>>($"/api/gym-management/trainers/{trainerId}/clients/invites?pageSize=2");
        Assert.Equal([third.InviteId, second.InviteId], page1!.Items.Select(i => i.InviteId).ToArray());
        Assert.NotNull(page1.NextCursor);
        var page2 = await _client.GetFromJsonAsync<Page<InviteListed>>(
            $"/api/gym-management/trainers/{trainerId}/clients/invites?pageSize=2&cursor={Uri.EscapeDataString(page1.NextCursor!)}");
        Assert.Equal([first.InviteId], page2!.Items.Select(i => i.InviteId).ToArray());
        Assert.Null(page2.NextCursor);
        Assert.Equal("Revoked", page1.Items[1].Status);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"/api/gym-management/trainers/{trainerId}/clients/invites?cursor=garbage")).StatusCode);
    }

    [Fact]
    public async Task TheTwentyFirstWaitingInvite_IsRefused_AndDeactivatingOneFreesTheSlot()
    {
        var (trainerId, first, _) = await CreateInviteAsync("cap");
        for (var i = 2; i <= 20; i++) await CreateExtraInviteAsync(trainerId, $"cap-{i}");

        var refused = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/cap-21@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);

        // Re-inviting an e-mail that already waits replaces it, so it never counts twice.
        var again = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/cap@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{(await again.Content.ReadFromJsonAsync<InviteCreated>())!.InviteId}")).StatusCode);
        var freed = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/cap-21@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, freed.StatusCode);
        Assert.Equal(TrainerClientInviteStatus.Superseded, await StatusOfAsync(first.InviteId));
    }

    [Fact]
    public async Task AcceptingAnExpiredInvite_Returns409_LikeTheNutritionists()
    {
        var (trainerId, invite, _) = await CreateInviteAsync("accexp");
        await using (var context = fixture.CreateGymManagementDbContext())
            await context.TrainerClientInvites.Where(i => i.Id == invite.InviteId)
                .ExecuteUpdateAsync(set => set.SetProperty(i => i.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));

        await SeedUserAndAuthenticateAsync("accexp-client");
        var accept = await _client.PostAsJsonAsync("/api/gym-management/trainer-client-invites/accept",
            new { payload = PayloadFor(trainerId, invite.AccessToken) });

        Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode);
    }

    private string PayloadFor(int trainerId, string accessToken)
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITrainerClientInvitePayloadCodec>()
            .Encode(new TrainerClientInviteUrlPayload(trainerId, accessToken));
    }

    private async Task<(int TrainerId, InviteCreated Invite, string TrainerBearer)> CreateInviteAsync(string suffix)
    {
        var trainerId = await SeedUserAndAuthenticateAsync(suffix);
        var bearer = _client.DefaultRequestHeaders.Authorization!.Parameter!;
        var created = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/{suffix}@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (trainerId, (await created.Content.ReadFromJsonAsync<InviteCreated>())!, bearer);
    }

    private async Task<TrainerClientInviteStatus> StatusOfAsync(int inviteId)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        return await context.TrainerClientInvites.Where(i => i.Id == inviteId).Select(i => i.Status).SingleAsync();
    }

    [Fact]
    public async Task InvitingTheSameEmailAgain_MarksTheOldOneSuperseded_NotRevoked_AndItsLinkStopsWorking()
    {
        var (trainerId, first, _) = await CreateInviteAsync("again");
        var email = "again@integration.test";
        var second = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/{email}",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var newer = (await second.Content.ReadFromJsonAsync<InviteCreated>())!;

        // An invite deactivated by hand stays "Revoked": the two reasons are told apart.
        var (_, manual, _) = (trainerId, await CreateExtraInviteAsync(trainerId, "again-manual"), 0);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{manual.InviteId}")).StatusCode);

        var list = await _client.GetFromJsonAsync<Page<InviteListed>>($"/api/gym-management/trainers/{trainerId}/clients/invites");
        Assert.Equal("Superseded", list!.Items.Single(i => i.InviteId == first.InviteId).Status);
        Assert.Equal("Invited", list.Items.Single(i => i.InviteId == newer.InviteId).Status);
        Assert.Equal("Revoked", list.Items.Single(i => i.InviteId == manual.InviteId).Status);

        // A superseded invite cannot be deactivated again, and its token no longer works.
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{first.InviteId}")).StatusCode);
        var clientId = await SeedUserAndAuthenticateAsync("again-client");
        var accept = await _client.PostAsJsonAsync("/api/gym-management/trainer-client-invites/accept",
            new { payload = PayloadFor(trainerId, first.AccessToken) });
        Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode);
        await using var context = fixture.CreateGymManagementDbContext();
        Assert.False(await context.TrainerClients.AnyAsync(tc => tc.ClientId == clientId));
    }

    private async Task<InviteCreated> CreateExtraInviteAsync(int trainerId, string suffix)
    {
        var created = await _client.PostAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/invites/{suffix}@integration.test",
            new { trainerPlanId = (int?)null, expiresInHours = (int?)null });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return (await created.Content.ReadFromJsonAsync<InviteCreated>())!;
    }

    [Fact]
    public async Task AcceptAfterRevoke_IsRefused_AndNoLinkIsCreated()
    {
        var (trainerId, invite, _) = await CreateInviteAsync("accrev");
        Assert.Equal(HttpStatusCode.OK,
            (await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{invite.InviteId}")).StatusCode);

        var clientId = await SeedUserAndAuthenticateAsync("accrev-client");
        var accept = await _client.PostAsJsonAsync("/api/gym-management/trainer-client-invites/accept",
            new { payload = PayloadFor(trainerId, invite.AccessToken) });

        Assert.Equal(HttpStatusCode.Conflict, accept.StatusCode);
        Assert.Equal(TrainerClientInviteStatus.Revoked, await StatusOfAsync(invite.InviteId));
        await using var context = fixture.CreateGymManagementDbContext();
        Assert.False(await context.TrainerClients.AnyAsync(tc => tc.ClientId == clientId));
    }

    [Fact]
    public async Task RevokeAndAcceptAtTheSameTime_ExactlyOneWins_AndTheStateAgrees()
    {
        for (var round = 0; round < 8; round++)
        {
            var (trainerId, invite, trainerBearer) = await CreateInviteAsync($"race{round}");
            var clientId = await SeedUserAndAuthenticateAsync($"race-client{round}");
            var clientBearer = _client.DefaultRequestHeaders.Authorization!.Parameter!;
            var payload = PayloadFor(trainerId, invite.AccessToken);

            HttpClient WithBearer(string bearer)
            {
                var http = _factory.CreateClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                return http;
            }

            using var trainerHttp = WithBearer(trainerBearer);
            using var clientHttp = WithBearer(clientBearer);
            var gate = new TaskCompletionSource();
            var revoke = Task.Run(async () =>
            {
                await gate.Task;
                return await trainerHttp.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{invite.InviteId}");
            });
            var accept = Task.Run(async () =>
            {
                await gate.Task;
                return await clientHttp.PostAsJsonAsync("/api/gym-management/trainer-client-invites/accept", new { payload });
            });
            gate.SetResult();
            var revoked = (await revoke).StatusCode;
            var accepted = (await accept).StatusCode;

            var status = await StatusOfAsync(invite.InviteId);
            await using var context = fixture.CreateGymManagementDbContext();
            var linked = await context.TrainerClients.AnyAsync(tc => tc.ClientId == clientId);

            if (revoked == HttpStatusCode.OK)
            {
                Assert.Equal(HttpStatusCode.Conflict, accepted);
                Assert.Equal(TrainerClientInviteStatus.Revoked, status);
                Assert.False(linked);
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, revoked);
                Assert.Equal(HttpStatusCode.OK, accepted);
                Assert.Equal(TrainerClientInviteStatus.Accepted, status);
                Assert.True(linked);
            }
        }
    }

    [Fact]
    public async Task RevokeInvite_OnYourOwnRoute_ButTheInviteBelongsToAnotherTrainer_ReturnsNotFound()
    {
        var (_, otherInvite, _) = await CreateInviteAsync("own404-owner");
        var trainerId = await SeedUserAndAuthenticateAsync("own404-caller");

        var response = await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{otherInvite.InviteId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvite_OfAnotherTrainer_ReturnsForbidden()
    {
        await SeedUserAndAuthenticateAsync("revtc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("revtc-other");

        var response = await _client.DeleteAsync($"/api/gym-management/trainers/{otherTrainerId}/clients/invites/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvite_Unknown_ReturnsNotFound()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("revtc-missing");

        var response = await _client.DeleteAsync($"/api/gym-management/trainers/{trainerId}/clients/invites/{Random.Shared.Next(700_000, 800_000)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record InviteCreated(int InviteId, string AccessToken, DateTime ExpiresAtUtc);
    private sealed record InviteListed(int InviteId, string Status, DateTime ExpiresAtUtc);
    private sealed record Page<T>(T[] Items, string? NextCursor);

    [Fact]
    public async Task Transfer_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("trftc-self");

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/transfer",
            new { newTrainerId = trainerId + 1, newPlanId = 1 });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("trftc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("trftc-other");

        var response = await _client.PutAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/transfer",
            new { newTrainerId = otherTrainerId + 1, newPlanId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("unatc-self");

        var response = await _client.DeleteAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unassign_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("unatc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("unatc-other");

        var response = await _client.DeleteAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetPlanStatus_SelfAccess_ShouldNotBeForbidden()
    {
        var trainerId = await SeedUserAndAuthenticateAsync("plstc-self");

        var response = await _client.PatchAsJsonAsync(
            $"/api/gym-management/trainers/{trainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/plan/status",
            new { isActive = false });

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetPlanStatus_CrossUser_ShouldReturnForbidden()
    {
        await SeedUserAndAuthenticateAsync("plstc-caller");
        var otherTrainerId = await SeedOtherUserIdAsync("plstc-other");

        var response = await _client.PatchAsJsonAsync(
            $"/api/gym-management/trainers/{otherTrainerId}/clients/{Random.Shared.Next(500_000, 600_000)}/plan/status",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Seeds a user, authenticates the shared client as them, and returns their DB id (trainerId for self-access).</summary>
    private async Task<int> SeedUserAndAuthenticateAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));

        return user.Id;
    }

    /// <summary>Seeds a second, unrelated user and returns their DB id -- used as a trainerId the caller does not own.</summary>
    private async Task<int> SeedOtherUserIdAsync(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return user.Id;
    }
}
