using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.AcceptTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;
using ShapeUp.Features.GymManagement.TrainerClients.RevokeTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.Shared;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.ListInvites;
using ShapeUp.Features.Nutrition.Clients.RevokeInvite;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using UnitTests.Support;

namespace UnitTests.Domains.Invites;

/// <summary>
/// The deadline of an invite is exclusive: at <c>ExpiresAtUtc == now</c> the invite is already expired, and one tick
/// before it is still alive. Every place that decides this is pinned on the exact tick, for both kinds of invite.
/// </summary>
public class InviteExpiryBoundaryTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly FixedTimeProvider Clock = new(Now);

    // ---- nutrition invites -------------------------------------------------------------------------------------

    private static ProfessionalClientInvite NutritionInvite(
        DateTime expires, ProfessionalClientInviteStatus status = ProfessionalClientInviteStatus.Pending) => new()
    {
        Id = 5,
        ProfessionalUserId = 1,
        RelationshipType = "Nutrition",
        TokenHash = InviteNutritionClientHandler.ComputeHash("tok"),
        Status = status,
        ExpiresAtUtc = expires
    };

    [Theory]
    [InlineData(-1, "Expired")]
    [InlineData(0, "Expired")]
    [InlineData(1, "Invited")]
    public void Nutrition_Status_FlipsToExpiredExactlyAtTheDeadline(long ticksFromNow, string expected)
    {
        var invite = NutritionInvite(Now.AddTicks(ticksFromNow));

        Assert.Equal(expected, ListNutritionInvitesHandler.StatusOf(invite, Now));
    }

    [Theory]
    [InlineData(ProfessionalClientInviteStatus.Accepted)]
    [InlineData(ProfessionalClientInviteStatus.Revoked)]
    public void Nutrition_Status_OfAFinishedInviteNeverBecomesExpired(ProfessionalClientInviteStatus status)
    {
        foreach (var ticks in new long[] { -1, 0, 1 })
            Assert.Equal(status.ToString(), ListNutritionInvitesHandler.StatusOf(NutritionInvite(Now.AddTicks(ticks), status), Now));
    }

    [Fact]
    public async Task Nutrition_List_UsesTheClockOnEveryRow()
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var invites = new Mock<IProfessionalClientInviteRepository>();
        invites.Setup(i => i.ListAsync(1, "Nutrition", null, 21, default)).ReturnsAsync(
        [
            NutritionInvite(Now.AddTicks(1)),
            NutritionInvite(Now),
            NutritionInvite(Now.AddTicks(-1)),
        ]);

        var page = await new ListNutritionInvitesHandler(capabilities.Object, invites.Object, Clock)
            .HandleAsync(new ListNutritionInvitesQuery(null, null), 1, default);

        Assert.Equal(["Invited", "Expired", "Expired"], page.Value!.Items.Select(i => i.Status).ToArray());
    }

    [Theory]
    [InlineData(null, 21)]
    [InlineData(0, 21)]
    [InlineData(-5, 21)]
    [InlineData(1, 2)]
    [InlineData(100, 101)]
    [InlineData(101, 101)]
    [InlineData(100000, 101)]
    public async Task Nutrition_List_NormalisesThePageSizeAtItsEdges(int? asked, int expectedFetch)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var invites = new Mock<IProfessionalClientInviteRepository>();
        invites.Setup(i => i.ListAsync(1, "Nutrition", null, expectedFetch, default)).ReturnsAsync([]);

        var result = await new ListNutritionInvitesHandler(capabilities.Object, invites.Object, Clock)
            .HandleAsync(new ListNutritionInvitesQuery(null, asked), 1, default);

        Assert.True(result.IsSuccess);
        invites.Verify(i => i.ListAsync(1, "Nutrition", null, expectedFetch, default), Times.Once);
    }

    [Theory]
    [InlineData(3, 3, false)]
    [InlineData(3, 2, true)]
    [InlineData(1, 1, false)]
    [InlineData(0, 20, false)]
    public async Task Nutrition_List_OnlyAsksForAnotherPageWhenThereIsOneMoreRow(int rows, int pageSize, bool hasNext)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var invites = new Mock<IProfessionalClientInviteRepository>();
        var all = Enumerable.Range(1, rows).Reverse()
            .Select(id => new ProfessionalClientInvite { Id = id, RelationshipType = "Nutrition", TokenHash = "h" + id, ExpiresAtUtc = Now.AddDays(1) })
            .ToList();
        invites.Setup(i => i.ListAsync(1, "Nutrition", null, pageSize + 1, default))
            .ReturnsAsync(all.Take(pageSize + 1).ToList());

        var page = await new ListNutritionInvitesHandler(capabilities.Object, invites.Object, Clock)
            .HandleAsync(new ListNutritionInvitesQuery(null, pageSize), 1, default);

        Assert.Equal(Math.Min(rows, pageSize), page.Value!.Items.Length);
        Assert.Equal(hasNext, page.Value.NextCursor is not null);
    }

    [Theory]
    [InlineData(-1, 409)]
    [InlineData(0, 409)]
    [InlineData(1, 200)]
    public async Task Nutrition_Revoke_IsRefusedAtOrAfterTheDeadline(long ticksFromNow, int expectedStatus)
    {
        var invites = new Mock<IProfessionalClientInviteRepository>();
        invites.Setup(i => i.GetByIdAsync(5, default)).ReturnsAsync(NutritionInvite(Now.AddTicks(ticksFromNow)));
        invites.Setup(i => i.TryRevokeAsync(5, 1, default)).ReturnsAsync(true);

        var result = await new RevokeNutritionInviteHandler(invites.Object, Clock).HandleAsync(5, 1, default);

        Assert.Equal(expectedStatus == 200, result.IsSuccess);
        if (expectedStatus != 200)
            Assert.Equal(expectedStatus, result.Error!.StatusCode);
        invites.Verify(i => i.TryRevokeAsync(5, 1, default), Times.Exactly(expectedStatus == 200 ? 1 : 0));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Nutrition_Accept_IsRefusedAtOrAfterTheDeadline(long ticksFromNow, bool accepted)
    {
        var invites = new Mock<IProfessionalClientInviteRepository>();
        var relationships = new Mock<IProfessionalClientRelationshipRepository>();
        var invite = NutritionInvite(Now.AddTicks(ticksFromNow));
        invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);
        invites.Setup(i => i.TryAcceptAsync(5, 2, Now, default)).ReturnsAsync(true);
        relationships.Setup(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), default))
            .ReturnsAsync((ProfessionalClientRelationship r, CancellationToken _) => ShapeUp.Shared.Results.Result<ProfessionalClientRelationship>.Success(r));

        var result = await new AcceptNutritionInviteHandler(invites.Object, relationships.Object, Clock)
            .HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.Equal(accepted, result.IsSuccess);
        if (!accepted)
            Assert.Equal(409, result.Error!.StatusCode);
        invites.Verify(i => i.TryAcceptAsync(5, 2, Now, default), Times.Exactly(accepted ? 1 : 0));
    }

    [Fact]
    public async Task Nutrition_Invite_ExpiresExactlySevenDaysAfterTheClock()
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var invites = new Mock<IProfessionalClientInviteRepository>();
        ProfessionalClientInvite? stored = null;
        invites.Setup(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), default))
            .Callback<ProfessionalClientInvite, CancellationToken>((i, _) => stored = i);

        var result = await new InviteNutritionClientHandler(capabilities.Object, invites.Object, Clock).HandleAsync(1, null, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddDays(7), stored!.ExpiresAtUtc);
        Assert.Equal(Now, stored.CreatedAtUtc);
        Assert.Equal(Now.AddDays(7), result.Value!.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(21, false)]
    public async Task Nutrition_Invite_PendingLimitIsExactlyTwenty(int pending, bool allowed)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var invites = new Mock<IProfessionalClientInviteRepository>();
        invites.Setup(i => i.CountPendingAsync(1, "Nutrition", Now, default)).ReturnsAsync(pending);

        var result = await new InviteNutritionClientHandler(capabilities.Object, invites.Object, Clock).HandleAsync(1, null, default);

        Assert.Equal(allowed, result.IsSuccess);
        if (!allowed)
            Assert.Equal(409, result.Error!.StatusCode);
        invites.Verify(i => i.CountPendingAsync(1, "Nutrition", Now, default), Times.Once);
    }

    // ---- trainer invites ---------------------------------------------------------------------------------------

    private static TrainerClientInvite TrainerInvite(
        DateTime expires, TrainerClientInviteStatus status = TrainerClientInviteStatus.Invited, int id = 9) => new()
    {
        Id = id,
        TrainerId = 7,
        InviteeEmail = "a@x.com",
        Status = status,
        ExpiresAtUtc = expires
    };

    [Theory]
    [InlineData(-1, "Expired")]
    [InlineData(0, "Expired")]
    [InlineData(1, "Invited")]
    public async Task Trainer_List_FlipsToExpiredExactlyAtTheDeadline(long ticksFromNow, string expected)
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.ListAsync(7, null, 21, default)).ReturnsAsync([TrainerInvite(Now.AddTicks(ticksFromNow))]);

        var result = await new GetTrainerClientInvitesHandler(repo.Object, Clock).HandleAsync(new GetTrainerClientInvitesQuery(7, null, null), default);

        Assert.Equal(expected, result.Value!.Items.Single().Status);
    }

    [Theory]
    [InlineData(TrainerClientInviteStatus.Accepted)]
    [InlineData(TrainerClientInviteStatus.Revoked)]
    [InlineData(TrainerClientInviteStatus.Superseded)]
    [InlineData(TrainerClientInviteStatus.Expired)]
    public async Task Trainer_List_KeepsTheStatusOfAFinishedInviteAtEveryEdge(TrainerClientInviteStatus status)
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.ListAsync(7, null, 21, default)).ReturnsAsync(
            [TrainerInvite(Now.AddTicks(-1), status, 1), TrainerInvite(Now, status, 2), TrainerInvite(Now.AddTicks(1), status, 3)]);

        var result = await new GetTrainerClientInvitesHandler(repo.Object, Clock).HandleAsync(new GetTrainerClientInvitesQuery(7, null, null), default);

        Assert.All(result.Value!.Items, invite => Assert.Equal(status.ToString(), invite.Status));
    }

    [Fact]
    public async Task Trainer_Revoke_AsksTheDatabaseWithTheClockInstant()
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.GetByIdAsync(9, default)).ReturnsAsync(TrainerInvite(Now.AddTicks(1)));
        repo.Setup(r => r.TryRevokeAsync(9, 7, Now, default)).ReturnsAsync(true);

        var result = await new RevokeTrainerClientInviteHandler(repo.Object, Clock).HandleAsync(7, 9, default);

        Assert.True(result.IsSuccess);
        repo.Verify(r => r.TryRevokeAsync(9, 7, Now, default), Times.Once);
    }

    [Theory]
    [InlineData(-1, TrainerClientInviteStatus.Expired, false)]
    [InlineData(0, TrainerClientInviteStatus.Expired, false)]
    [InlineData(1, TrainerClientInviteStatus.Invited, true)]
    public async Task Trainer_Accept_IsRefusedAtOrAfterTheDeadline(long ticksFromNow, TrainerClientInviteStatus finalStatus, bool accepted)
    {
        const string token = "valid-token";
        var tokenHash = ShapeUp.Features.GymManagement.Shared.Security.TrainerClientInviteTokenCodec.ComputeHash(token);
        var invite = TrainerInvite(Now.AddTicks(ticksFromNow));
        invite.AccessTokenHash = tokenHash;
        var invites = new Mock<ITrainerClientInviteRepository>();
        var trainerClients = new Mock<ITrainerClientRepository>();
        var codec = new Mock<ITrainerClientInvitePayloadCodec>();
        codec.Setup(c => c.Decode("payload"))
            .Returns(ShapeUp.Shared.Results.Result<TrainerClientInviteUrlPayload>.Success(new TrainerClientInviteUrlPayload(7, token)));
        invites.Setup(r => r.GetByTokenHashAsync(tokenHash, default)).ReturnsAsync(invite);
        invites.Setup(r => r.TryAcceptAsync(9, 300, Now, default)).ReturnsAsync(true);
        trainerClients.Setup(r => r.GetByClientIdAsync(300, default)).ReturnsAsync((TrainerClient?)null);
        var gym = new Mock<IGymClientRepository>();
        gym.Setup(r => r.GetByUserIdAsync(300, default)).ReturnsAsync((GymClient?)null);
        var roles = new Mock<IUserPlatformRoleRepository>();
        roles.Setup(r => r.GetByUserIdAndRoleAsync(300, It.IsAny<PlatformRoleType>(), default)).ReturnsAsync((UserPlatformRole?)null);

        var handler = new AcceptTrainerClientInviteHandler(
            invites.Object, trainerClients.Object, new Mock<ITrainerPlanRepository>().Object, gym.Object, roles.Object,
            codec.Object, new AcceptTrainerClientInviteValidator(), Clock);
        var result = await handler.HandleAsync(new AcceptTrainerClientInviteCommand("payload"), 300, default);

        Assert.Equal(accepted, result.IsSuccess);
        Assert.Equal(finalStatus, invite.Status);
        if (!accepted)
            Assert.Equal(409, result.Error!.StatusCode);
        invites.Verify(r => r.TryAcceptAsync(9, 300, Now, default), Times.Exactly(accepted ? 1 : 0));
        trainerClients.Verify(r => r.AddAsync(It.IsAny<TrainerClient>(), default), Times.Exactly(accepted ? 1 : 0));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)]
    public async Task Trainer_Generate_ExpiresExactlyTheRequestedHoursAfterTheClock(int hours)
    {
        var (handler, repo) = GenerateSetup();
        TrainerClientInvite? stored = null;
        repo.Setup(r => r.AddAsync(It.IsAny<TrainerClientInvite>(), default))
            .Callback<TrainerClientInvite, CancellationToken>((i, _) => stored = i).Returns(Task.CompletedTask);

        var command = new GenerateTrainerClientInviteCommand(null, hours);
        command.SetClientEmail("new@x.com");
        command.SetTrainerName("Carlos");
        var result = await handler.HandleAsync(command, 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Now.AddHours(hours), stored!.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(168, true)]
    [InlineData(169, false)]
    [InlineData(-1, false)]
    public void Trainer_Generate_ExpiryHoursAreBetweenOneAnd168(int hours, bool valid)
    {
        var command = new GenerateTrainerClientInviteCommand(null, hours);
        command.SetClientEmail("new@x.com");
        command.SetTrainerName("Carlos");

        Assert.Equal(valid, new GenerateTrainerClientInviteValidator().Validate(command).IsValid);
    }

    [Theory]
    [InlineData(1, TrainerClientInviteStatus.Superseded)]
    [InlineData(0, TrainerClientInviteStatus.Invited)]
    [InlineData(-1, TrainerClientInviteStatus.Invited)]
    public async Task Trainer_Generate_OnlyReplacesTheInviteStillWithinItsDeadline(long ticksFromNow, TrainerClientInviteStatus expected)
    {
        var (handler, repo) = GenerateSetup();
        var previous = TrainerInvite(Now.AddTicks(ticksFromNow));
        repo.Setup(r => r.GetActiveByTrainerAndEmailAsync(7, "new@x.com", default)).ReturnsAsync(previous);
        repo.Setup(r => r.UpdateAsync(previous, default)).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddAsync(It.IsAny<TrainerClientInvite>(), default)).Returns(Task.CompletedTask);

        var command = new GenerateTrainerClientInviteCommand(null, 24);
        command.SetClientEmail("new@x.com");
        command.SetTrainerName("Carlos");
        var result = await handler.HandleAsync(command, 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, previous.Status);
        repo.Verify(r => r.UpdateAsync(previous, default),
            Times.Exactly(expected == TrainerClientInviteStatus.Superseded ? 1 : 0));
    }

    private static (GenerateTrainerClientInviteHandler Handler, Mock<ITrainerClientInviteRepository> Repo) GenerateSetup()
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.GetActiveByTrainerAndEmailAsync(It.IsAny<int>(), It.IsAny<string>(), default))
            .ReturnsAsync((TrainerClientInvite?)null);
        repo.Setup(r => r.AddAsync(It.IsAny<TrainerClientInvite>(), default)).Returns(Task.CompletedTask);
        var email = new Mock<ShapeUp.Features.Notifications.Shared.Abstractions.IEmailNotificationSender>();
        email.Setup(e => e.SendTemplateAsync(It.IsAny<ShapeUp.Features.Notifications.Shared.Models.SendTemplateEmailRequest>(), default))
            .ReturnsAsync(ShapeUp.Shared.Results.Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("id")));
        var url = new Mock<ITrainerClientInviteRegisterUrlBuilder>();
        url.Setup(u => u.BuildRegisterUrl(It.IsAny<int>(), It.IsAny<string>())).Returns("https://x/register");
        var options = Microsoft.Extensions.Options.Options.Create(new TrainerClientInviteEmailOptions { TemplateId = "t", Subject = "s" });
        var handler = new GenerateTrainerClientInviteHandler(
            repo.Object, new Mock<ITrainerPlanRepository>().Object, email.Object, url.Object, options,
            new GenerateTrainerClientInviteValidator(), Clock);
        return (handler, repo);
    }
}
