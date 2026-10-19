using Microsoft.Extensions.Options;
using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;
using ShapeUp.Features.GymManagement.TrainerClients.Shared;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.ListInvites;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Invites;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;
using UnitTests.Support;

namespace UnitTests.Domains.Invites;

/// <summary>Nutritionist and trainer invites follow the same rules (<see cref="InviteRules"/>): lifetime, cap, vocabulary and paging.</summary>
public class InviteParityTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly FixedTimeProvider Clock = new(Now);

    // ---- InviteRules itself ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(168, true)]
    [InlineData(169, false)]
    [InlineData(-1, false)]
    public void Rules_LifetimeLimitsAreInclusive(int hours, bool valid) =>
        Assert.Equal(valid, InviteRules.IsValidLifetime(hours));

    [Fact]
    public void Rules_DefaultLifetimeIsSevenDays()
    {
        Assert.Equal(168, InviteRules.DefaultLifetimeHours);
        Assert.Equal(Now.AddDays(7), InviteRules.ExpiresAt(Now, null));
        Assert.Equal(Now.AddHours(5), InviteRules.ExpiresAt(Now, 5));
    }

    [Theory]
    [InlineData("Invited", -1, "Expired")]
    [InlineData("Invited", 0, "Expired")]
    [InlineData("Invited", 1, "Invited")]
    [InlineData("Accepted", -1, "Accepted")]
    [InlineData("Revoked", 0, "Revoked")]
    [InlineData("Superseded", -1, "Superseded")]
    public void Rules_StatusNameUsesAnExclusiveDeadline(string stored, long ticks, string expected) =>
        Assert.Equal(expected, InviteRules.StatusName(stored, Now.AddTicks(ticks), Now));

    // ---- same lifetime ----------------------------------------------------------------------------------------

    private static InviteNutritionClientHandler NutritionInviteHandler(out Mock<IProfessionalClientInviteRepository> invites, int pending = 0)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        invites = new Mock<IProfessionalClientInviteRepository>();
        invites.Setup(i => i.CountPendingAsync(1, "Nutrition", Now, default)).ReturnsAsync(pending);
        return new InviteNutritionClientHandler(capabilities.Object, invites.Object, Clock);
    }

    [Fact]
    public async Task Nutrition_Invite_DefaultsToTheSharedLifetime()
    {
        var result = await NutritionInviteHandler(out _).HandleAsync(1, null, default);

        Assert.Equal(InviteRules.ExpiresAt(Now, null), result.Value!.ExpiresAtUtc);
    }

    [Fact]
    public async Task Nutrition_Invite_AcceptsACustomLifetime()
    {
        var result = await NutritionInviteHandler(out _).HandleAsync(1, new InviteNutritionClientCommand(24), default);

        Assert.Equal(Now.AddHours(24), result.Value!.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(169)]
    public async Task Nutrition_Invite_RejectsALifetimeOutsideTheSharedLimits(int hours)
    {
        var handler = NutritionInviteHandler(out var invites);

        var result = await handler.HandleAsync(1, new InviteNutritionClientCommand(hours), default);

        Assert.Equal(400, result.Error!.StatusCode);
        invites.Verify(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (GenerateTrainerClientInviteHandler Handler, Mock<ITrainerClientInviteRepository> Invites, GenerateTrainerClientInviteCommand Command)
        TrainerInviteHandler(int waiting = 0, TrainerClientInvite? active = null, int? expiresInHours = null)
    {
        var invites = new Mock<ITrainerClientInviteRepository>();
        invites.Setup(r => r.GetActiveByTrainerAndEmailAsync(7, "c@x.com", default)).ReturnsAsync(active);
        int? exceptId = active?.Id;
        invites.Setup(r => r.CountWaitingAsync(7, Now, exceptId, default)).ReturnsAsync(waiting);
        var sender = new Mock<IEmailNotificationSender>();
        sender.Setup(s => s.SendTemplateAsync(It.IsAny<SendTemplateEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("id")));
        var url = new Mock<ITrainerClientInviteRegisterUrlBuilder>();
        url.Setup(b => b.BuildRegisterUrl(7, It.IsAny<string>())).Returns("https://x");
        var handler = new GenerateTrainerClientInviteHandler(
            invites.Object, new Mock<ITrainerPlanRepository>().Object, sender.Object, url.Object,
            Options.Create(new TrainerClientInviteEmailOptions { TemplateId = "t", Subject = "s" }),
            new GenerateTrainerClientInviteValidator(), Clock);
        var command = new GenerateTrainerClientInviteCommand(null, expiresInHours);
        command.SetClientEmail("c@x.com");
        command.SetTrainerName("Treinador");
        return (handler, invites, command);
    }

    [Fact]
    public async Task Trainer_Invite_DefaultsToTheSameLifetimeAsTheNutritionist()
    {
        var (handler, _, command) = TrainerInviteHandler();

        var result = await handler.HandleAsync(command, 7, default);

        Assert.Equal(InviteRules.ExpiresAt(Now, null), result.Value!.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(168)]
    public async Task Trainer_Invite_AcceptsTheSharedLimits(int hours)
    {
        var (handler, _, command) = TrainerInviteHandler(expiresInHours: hours);

        var result = await handler.HandleAsync(command, 7, default);

        Assert.Equal(Now.AddHours(hours), result.Value!.ExpiresAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)]
    public async Task Trainer_Invite_RejectsALifetimeOutsideTheSharedLimits(int hours)
    {
        var (handler, invites, command) = TrainerInviteHandler(expiresInHours: hours);

        var result = await handler.HandleAsync(command, 7, default);

        Assert.Equal(400, result.Error!.StatusCode);
        invites.Verify(r => r.AddAsync(It.IsAny<TrainerClientInvite>(), default), Times.Never);
    }

    // ---- same cap ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task BothProfessions_ShareTheSameCapAndTheSameMessage()
    {
        var nutrition = await NutritionInviteHandler(out _, pending: InviteRules.MaxWaitingInvites).HandleAsync(1, null, default);
        var (handler, invites, command) = TrainerInviteHandler(waiting: InviteRules.MaxWaitingInvites);
        var trainer = await handler.HandleAsync(command, 7, default);

        Assert.Equal(409, nutrition.Error!.StatusCode);
        Assert.Equal(409, trainer.Error!.StatusCode);
        Assert.Equal(nutrition.Error.Message, trainer.Error.Message);
        Assert.Equal(InviteRules.LimitMessage, trainer.Error.Message);
        invites.Verify(r => r.AddAsync(It.IsAny<TrainerClientInvite>(), default), Times.Never);
    }

    [Fact]
    public async Task BothProfessions_AllowTheLastFreeSlot()
    {
        var nutrition = await NutritionInviteHandler(out _, pending: InviteRules.MaxWaitingInvites - 1).HandleAsync(1, null, default);
        var (handler, _, command) = TrainerInviteHandler(waiting: InviteRules.MaxWaitingInvites - 1);
        var trainer = await handler.HandleAsync(command, 7, default);

        Assert.True(nutrition.IsSuccess);
        Assert.True(trainer.IsSuccess);
    }

    [Fact]
    public async Task Trainer_Reinviting_TheSameEmailDoesNotCountTheInviteItReplaces()
    {
        var active = new TrainerClientInvite
        {
            Id = 40, TrainerId = 7, InviteeEmail = "c@x.com", Status = TrainerClientInviteStatus.Invited, ExpiresAtUtc = Now.AddHours(3)
        };
        var (handler, invites, command) = TrainerInviteHandler(waiting: InviteRules.MaxWaitingInvites - 1, active: active);

        var result = await handler.HandleAsync(command, 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(TrainerClientInviteStatus.Superseded, active.Status);
        invites.Verify(r => r.CountWaitingAsync(7, Now, 40, default), Times.Once);
    }

    [Fact]
    public async Task Trainer_AtTheCap_DoesNotSupersedeAnything()
    {
        var active = new TrainerClientInvite
        {
            Id = 40, TrainerId = 7, InviteeEmail = "c@x.com", Status = TrainerClientInviteStatus.Invited, ExpiresAtUtc = Now.AddHours(3)
        };
        var (handler, invites, command) = TrainerInviteHandler(waiting: InviteRules.MaxWaitingInvites, active: active);

        var result = await handler.HandleAsync(command, 7, default);

        Assert.Equal(409, result.Error!.StatusCode);
        Assert.Equal(TrainerClientInviteStatus.Invited, active.Status);
        invites.Verify(r => r.UpdateAsync(It.IsAny<TrainerClientInvite>(), default), Times.Never);
    }

    // ---- same vocabulary and same paging ----------------------------------------------------------------------

    [Theory]
    [InlineData(ProfessionalClientInviteStatus.Pending, TrainerClientInviteStatus.Invited, 1, "Invited")]
    [InlineData(ProfessionalClientInviteStatus.Pending, TrainerClientInviteStatus.Invited, 0, "Expired")]
    [InlineData(ProfessionalClientInviteStatus.Pending, TrainerClientInviteStatus.Invited, -1, "Expired")]
    [InlineData(ProfessionalClientInviteStatus.Accepted, TrainerClientInviteStatus.Accepted, -1, "Accepted")]
    [InlineData(ProfessionalClientInviteStatus.Revoked, TrainerClientInviteStatus.Revoked, 1, "Revoked")]
    public async Task SameStateIsReportedWithTheSameWord(
        ProfessionalClientInviteStatus nutritionStatus, TrainerClientInviteStatus trainerStatus, long ticks, string expected)
    {
        var nutrition = ListNutritionInvitesHandler.StatusOf(
            new ProfessionalClientInvite { RelationshipType = "Nutrition", TokenHash = "h", Status = nutritionStatus, ExpiresAtUtc = Now.AddTicks(ticks) }, Now);

        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.ListAsync(7, null, 21, default)).ReturnsAsync(
            [new TrainerClientInvite { Id = 1, Status = trainerStatus, ExpiresAtUtc = Now.AddTicks(ticks) }]);
        var trainer = await new GetTrainerClientInvitesHandler(repo.Object, Clock)
            .HandleAsync(new GetTrainerClientInvitesQuery(7, null, null), default);

        Assert.Equal(expected, nutrition);
        Assert.Equal(expected, trainer.Value!.Items.Single().Status);
    }

    private static TrainerClientInvite[] TrainerInvites(int count, int fromId) =>
        Enumerable.Range(0, count)
            .Select(i => new TrainerClientInvite { Id = fromId - i, InviteeEmail = $"{fromId - i}@x.com", Status = TrainerClientInviteStatus.Invited, ExpiresAtUtc = Now.AddDays(1) })
            .ToArray();

    [Theory]
    [InlineData(null, 20)]
    [InlineData(5, 5)]
    [InlineData(1000, 100)]
    public async Task Trainer_List_UsesTheSamePageSizeRulesAsTheNutritionist(int? asked, int effective)
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.ListAsync(7, null, effective + 1, default)).ReturnsAsync(TrainerInvites(effective + 1, 500));

        var page = await new GetTrainerClientInvitesHandler(repo.Object, Clock)
            .HandleAsync(new GetTrainerClientInvitesQuery(7, null, asked), default);

        Assert.Equal(effective, page.Value!.Items.Length);
        Assert.Equal(KeysetCursorCodec.EncodeLong(500 - effective + 1), page.Value.NextCursor);
    }

    [Fact]
    public async Task Trainer_List_LastPageHasNoCursor_AndTheCursorIsPassedAsBeforeId()
    {
        var repo = new Mock<ITrainerClientInviteRepository>();
        repo.Setup(r => r.ListAsync(7, 30, 21, default)).ReturnsAsync(TrainerInvites(3, 29));

        var page = await new GetTrainerClientInvitesHandler(repo.Object, Clock)
            .HandleAsync(new GetTrainerClientInvitesQuery(7, KeysetCursorCodec.EncodeLong(30), null), default);

        Assert.Equal(3, page.Value!.Items.Length);
        Assert.Null(page.Value.NextCursor);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("%%%")]
    public async Task Trainer_List_RejectsAnInvalidCursor(string cursor)
    {
        var repo = new Mock<ITrainerClientInviteRepository>();

        var page = await new GetTrainerClientInvitesHandler(repo.Object, Clock)
            .HandleAsync(new GetTrainerClientInvitesQuery(7, cursor, null), default);

        Assert.Equal(400, page.Error!.StatusCode);
        repo.Verify(r => r.ListAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>(), default), Times.Never);
    }
}
