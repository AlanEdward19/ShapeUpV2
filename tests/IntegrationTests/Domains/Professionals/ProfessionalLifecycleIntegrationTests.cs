namespace IntegrationTests.Domains.Professionals;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Relationships.Shared.Entities;

/// <summary>Registration e-mails and "ajuste solicitado", nutrition invite by e-mail, report/block and account deletion, over real HTTP and SQL Server.</summary>
[Collection("SQL Server Write Operations")]
public sealed class ProfessionalLifecycleIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private TestEmailNotificationSender _emails = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        _emails = _factory.Services.GetRequiredService<TestEmailNotificationSender>();
        var admin = await SeedUserAsync(asAdmin: true);
        Authorize(admin.Token);
        await _client.PutAsJsonAsync("/api/platform/feature-flags/notifications.email-enabled", new { enabled = true });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // Credentials: e-mails and request-changes ---------------------------------------------------

    [Fact]
    public async Task Credential_NeedsChanges_FullCycle_SendsOneEmailPerStep()
    {
        var pro = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);
        _emails.Clear();

        Authorize(pro.Token);
        var created = await (await Submit("Nutritionist", "CRN", "SP", "71111")).Content.ReadFromJsonAsync<Credential>();
        Assert.Equal("UnderReview", created!.Status);
        Assert.Single(_emails.Snapshot(), m => m.To == pro.Email && m.Subject.Contains("Recebemos"));

        Authorize(admin.Token);
        var changes = await _client.PostAsJsonAsync($"/api/credentials/{created.Id}/request-changes", new { message = "Número ilegível, confira." });
        Assert.Equal(HttpStatusCode.OK, changes.StatusCode);
        Assert.Equal("NeedsChanges", (await changes.Content.ReadFromJsonAsync<Credential>())!.Status);
        var changeMail = Assert.Single(_emails.Snapshot(), m => m.Subject.Contains("ajuste"));
        Assert.Equal(pro.Email, changeMail.To);
        Assert.Contains("Número ilegível", changeMail.Html);

        // A second request-changes is refused: nothing is sent twice.
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync($"/api/credentials/{created.Id}/request-changes", new { message = "x" })).StatusCode);
        Assert.Single(_emails.Snapshot(), m => m.Subject.Contains("ajuste"));

        Authorize(pro.Token);
        var mine = await _client.GetFromJsonAsync<Credential[]>("/api/credentials/me");
        var current = Assert.Single(mine!);
        Assert.Equal("NeedsChanges", current.Status);
        Assert.Equal("Número ilegível, confira.", current.ChangesRequestedMessage);

        // The user cannot open a second registration while this one is open.
        Assert.Equal(HttpStatusCode.Conflict, (await Submit("Nutritionist", "CRN", "SP", "72222")).StatusCode);

        var resubmit = await _client.PutAsJsonAsync($"/api/credentials/{created.Id}", new { credentialNumber = "71119", issuingRegion = "RJ" });
        Assert.Equal(HttpStatusCode.OK, resubmit.StatusCode);
        var resubmitted = await resubmit.Content.ReadFromJsonAsync<Credential>();
        Assert.Equal("UnderReview", resubmitted!.Status);
        Assert.Null(resubmitted.ChangesRequestedMessage);
        Assert.Equal(2, _emails.Snapshot().Count(m => m.Subject.Contains("Recebemos")));

        Authorize(admin.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/credentials/{created.Id}/approve", null)).StatusCode);
        Assert.Single(_emails.Snapshot(), m => m.Subject.Contains("aprovado"));

        Authorize(pro.Token);
        Assert.True((await _client.GetFromJsonAsync<Capabilities>("/api/professional-capabilities/me"))!.Nutrition);
    }

    [Fact]
    public async Task Credential_Resubmit_ByAnotherUser_Is404_AndWhenNotNeedingChanges_Is409()
    {
        var pro = await SeedUserAsync();
        var other = await SeedUserAsync();
        Authorize(pro.Token);
        var created = await (await Submit("PersonalTrainer", "CREF", "SP", "81111")).Content.ReadFromJsonAsync<Credential>();
        var body = new { credentialNumber = "81112", issuingRegion = "SP" };

        Assert.Equal(HttpStatusCode.Conflict, (await _client.PutAsJsonAsync($"/api/credentials/{created!.Id}", body)).StatusCode);
        Authorize(other.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync($"/api/credentials/{created.Id}", body)).StatusCode);
    }

    [Fact]
    public async Task Credential_Reject_And_Revoke_SendEmail_AndRequestChangesNeedsAdmin()
    {
        var pro = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);
        Authorize(pro.Token);
        var created = await (await Submit("PersonalTrainer", "CREF", "MG", "91111")).Content.ReadFromJsonAsync<Credential>();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync($"/api/credentials/{created!.Id}/request-changes", new { message = "x" })).StatusCode);

        Authorize(admin.Token);
        _emails.Clear();
        await _client.PostAsJsonAsync($"/api/credentials/{created.Id}/reject", new { reason = "CREF inexistente" });
        var rejected = Assert.Single(_emails.Snapshot(), m => m.To == pro.Email);
        Assert.Contains("CREF inexistente", rejected.Html);

        Authorize(pro.Token);
        var second = await (await Submit("PersonalTrainer", "CREF", "MG", "91112")).Content.ReadFromJsonAsync<Credential>();
        Authorize(admin.Token);
        await _client.PostAsync($"/api/credentials/{second!.Id}/approve", null);
        _emails.Clear();
        await _client.PostAsJsonAsync($"/api/credentials/{second.Id}/revoke", new { reason = "Cassado" });
        var revoked = Assert.Single(_emails.Snapshot(), m => m.To == pro.Email);
        Assert.Contains("Cassado", revoked.Html);
    }

    // Nutrition invite by e-mail -----------------------------------------------------------------

    [Fact]
    public async Task NutritionInvite_WithEmail_SendsLink_ReinviteRevokesPrevious_AndLinkAccepts()
    {
        var nutri = await SeedNutritionistAsync();
        var client = await SeedUserAsync();
        Authorize(nutri.Token);
        _emails.Clear();

        var first = await _client.PostAsJsonAsync("/api/nutrition/clients/invites", new { clientEmail = client.Email });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<Invite>();
        Assert.True(firstBody!.EmailSent);
        var mail = Assert.Single(_emails.Snapshot(), m => m.To == client.Email);
        Assert.Contains("nutrition=", mail.Html);
        Assert.Contains(Uri.EscapeDataString(firstBody.Token), mail.Html.Replace("&amp;", "&"));

        var second = await (await _client.PostAsJsonAsync("/api/nutrition/clients/invites", new { clientEmail = client.Email })).Content.ReadFromJsonAsync<Invite>();
        var list = await _client.GetFromJsonAsync<InvitePage>("/api/nutrition/clients/invites?pageSize=50");
        Assert.Equal("Revoked", list!.Items.Single(i => i.InviteId == firstBody.InviteId).Status);
        Assert.Equal("Invited", list.Items.Single(i => i.InviteId == second!.InviteId).Status);

        // The old token no longer works; the new one does.
        Authorize(client.Token);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = firstBody.Token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = second!.Token })).StatusCode);
    }

    [Fact]
    public async Task NutritionInvite_WithoutEmail_StillWorksByLink_AndRejectsBadEmail()
    {
        var nutri = await SeedNutritionistAsync();
        Authorize(nutri.Token);
        _emails.Clear();

        var plain = await _client.PostAsync("/api/nutrition/clients/invites", null);
        var body = await plain.Content.ReadFromJsonAsync<Invite>();
        Assert.False(body!.EmailSent);
        Assert.Contains("nutrition=", body.InviteUrl);
        Assert.Empty(_emails.Snapshot());

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/nutrition/clients/invites", new { clientEmail = "nao-e-email" })).StatusCode);
    }

    // Report and block ---------------------------------------------------------------------------

    [Fact]
    public async Task Report_GoesToAdminQueue_AndCanBeResolvedOnce()
    {
        var reporter = await SeedUserAsync();
        var target = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);

        Authorize(reporter.Token);
        var report = await _client.PostAsJsonAsync("/api/safety/reports",
            new { targetType = "User", targetUserId = target.UserId, reason = "Harassment", details = "mensagens ofensivas" });
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);
        var created = await report.Content.ReadFromJsonAsync<Report>();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/safety/reports", new { targetType = "User", targetUserId = reporter.UserId, reason = "Spam" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/safety/reports", new { targetType = "User", targetUserId = target.UserId, reason = "Because" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/safety/reports")).StatusCode);

        Authorize(admin.Token);
        var queue = await _client.GetFromJsonAsync<ReportPage>("/api/safety/reports?pageSize=100");
        Assert.Contains(queue!.Items, r => r.Id == created!.Id && r.Status == "Open");
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync($"/api/safety/reports/{created!.Id}/resolve", new { decision = "Resolved", note = "conta suspensa" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync($"/api/safety/reports/{created.Id}/resolve", new { decision = "Dismissed" })).StatusCode);
    }

    [Fact]
    public async Task Block_EndsNutritionLink_BlocksNewInvites_AndCanBeUndone()
    {
        var nutri = await SeedNutritionistAsync();
        var client = await SeedUserAsync();
        Authorize(nutri.Token);
        var invite = await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<Invite>();
        Authorize(client.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = invite!.Token })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/safety/blocks/{nutri.UserId}", null)).StatusCode);
        await using (var relationships = fixture.CreateRelationshipsDbContext())
            Assert.DoesNotContain(relationships.ProfessionalClientRelationships,
                r => r.ClientUserId == client.UserId && r.Status == RelationshipStatus.Active);

        // Blocked in either direction: a fresh invite cannot be accepted.
        Authorize(nutri.Token);
        var again = await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<Invite>();
        Authorize(client.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = again!.Token })).StatusCode);

        var blocks = await _client.GetFromJsonAsync<Block[]>("/api/safety/blocks");
        Assert.Contains(blocks!, b => b.BlockedUserId == nutri.UserId);
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/safety/blocks/{nutri.UserId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = again.Token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsync($"/api/safety/blocks/{client.UserId}", null)).StatusCode);
    }

    // Account deletion ---------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAccount_RemovesUserDataEverywhere_AndKeepsOthers()
    {
        var nutri = await SeedNutritionistAsync();
        var client = await SeedUserAsync();
        var bystander = await SeedUserAsync();

        Authorize(nutri.Token);
        var invite = await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<Invite>();
        Authorize(client.Token);
        await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = invite!.Token });
        await _client.PostAsync($"/api/safety/blocks/{bystander.UserId}", null);
        await _client.PostAsJsonAsync("/api/safety/reports", new { targetType = "User", targetUserId = bystander.UserId, reason = "Spam" });
        Authorize(bystander.Token);
        await _client.PostAsJsonAsync("/api/safety/reports", new { targetType = "User", targetUserId = nutri.UserId, reason = "Spam" });

        Authorize(client.Token);
        var response = await _client.DeleteAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using (var auth = fixture.CreateAuthorizationDbContext())
        {
            Assert.False(await auth.Users.AnyAsync(u => u.Id == client.UserId));
            Assert.True(await auth.Users.AnyAsync(u => u.Id == nutri.UserId));
            Assert.True(await auth.Users.AnyAsync(u => u.Id == bystander.UserId));
        }

        await using (var relationships = fixture.CreateRelationshipsDbContext())
            Assert.DoesNotContain(relationships.ProfessionalClientRelationships, r => r.ClientUserId == client.UserId);
        await using (var safety = fixture.CreateSafetyDbContext())
        {
            Assert.DoesNotContain(safety.UserBlocks, b => b.BlockerUserId == client.UserId || b.BlockedUserId == client.UserId);
            Assert.DoesNotContain(safety.ContentReports, r => r.ReporterUserId == client.UserId);
            Assert.Contains(safety.ContentReports, r => r.ReporterUserId == bystander.UserId);
        }

        var firebase = (TestFirebaseService)_factory.Services.GetRequiredService<ShapeUp.Features.Authorization.Shared.Abstractions.IFirebaseService>();
        Assert.Contains(client.FirebaseUid, firebase.DeletedUids);
    }

    [Fact]
    public async Task DeleteAccount_ProfessionalWithCredentials_RemovesRegistrationAndRole()
    {
        var pro = await SeedNutritionistAsync();
        Authorize(pro.Token);

        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync("/api/users/me")).StatusCode);

        await using var credentials = fixture.CreateCredentialsDbContext();
        Assert.DoesNotContain(credentials.ProfessionalCredentials, c => c.UserId == pro.UserId);
        await using var gym = fixture.CreateGymManagementDbContext();
        Assert.DoesNotContain(gym.UserPlatformRoles, r => r.UserId == pro.UserId);
    }

    [Fact]
    public async Task DeleteAccount_GymOwnerWithMembers_IsRefusedAndNothingIsLost()
    {
        var owner = await SeedUserAsync();
        var member = await SeedUserAsync();
        await using (var gymContext = fixture.CreateGymManagementDbContext())
        {
            var gym = new Gym { OwnerId = owner.UserId, Name = "Academia teste" };
            var plan = new GymPlan { Gym = gym, Name = "Mensal" };
            gymContext.GymClients.Add(new GymClient { Gym = gym, GymPlan = plan, UserId = member.UserId });
            await gymContext.SaveChangesAsync();
        }

        Authorize(owner.Token);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync("/api/users/me")).StatusCode);

        await using var auth = fixture.CreateAuthorizationDbContext();
        Assert.True(await auth.Users.AnyAsync(u => u.Id == owner.UserId));
    }

    [Fact]
    public async Task DeleteAccount_IsAuthenticatedOnly()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/users/me")).StatusCode);
    }

    // Helpers ------------------------------------------------------------------------------------

    private Task<HttpResponseMessage> Submit(string profession, string authority, string region, string number) =>
        _client.PostAsJsonAsync("/api/credentials", new
        {
            professionType = profession, credentialNumber = number, issuingAuthority = authority, issuingRegion = region, country = "BR"
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

        return new TestUser(user.Id, user.Email, user.FirebaseUid, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private async Task<TestUser> SeedNutritionistAsync()
    {
        var pro = await SeedUserAsync();
        var admin = await SeedUserAsync(asAdmin: true);
        Authorize(pro.Token);
        var created = await (await Submit("Nutritionist", "CRN", "SP", Random.Shared.Next(100000, 999999).ToString())).Content.ReadFromJsonAsync<Credential>();
        Authorize(admin.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/credentials/{created!.Id}/approve", null)).StatusCode);
        return pro;
    }

    private sealed record TestUser(int UserId, string Email, string FirebaseUid, string Token);
    private sealed record Capabilities(bool Training, bool Nutrition);
    private sealed record Credential(int Id, string Status, string? ChangesRequestedMessage);
    private sealed record Invite(string Token, int InviteId, string? InviteUrl, bool EmailSent);
    private sealed record InviteItem(int InviteId, string Status);
    private sealed record InvitePage(InviteItem[] Items, string? NextCursor);
    private sealed record Report(int Id, string Status);
    private sealed record ReportPage(Report[] Items, string? NextCursor);
    private sealed record Block(int BlockedUserId);
}
