using Microsoft.EntityFrameworkCore;
using Moq;
using ShapeUp.Features.Nutrition.Fasting.DeclineRecommendation;
using ShapeUp.Features.Nutrition.Fasting.DeleteAgenda;
using ShapeUp.Features.Nutrition.Fasting.EndEarly;
using ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;
using ShapeUp.Features.Nutrition.Fasting.GetHistory;
using ShapeUp.Features.Nutrition.Fasting.PutAgenda;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class FastingLifecycleHandlerTests
{
    private const int User = FastingTestSupport.UserId;

    // 2026-06-15 16:00 UTC = 13:00 in Sao Paulo (UTC-3).
    private static readonly DateTime Now = new(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc);

    private static FastingAgenda Agenda(int eatingStartMinutes = 720) => new()
    {
        UserId = User, Protocol = "16:8", FastHours = 16, EatHours = 8,
        EatingStartMinutes = eatingStartMinutes, TimeZone = FastingTestSupport.SaoPaulo, UpdatedAtUtc = Now
    };

    private static FastingStateReader Reader(NutritionDbContext db, DateTime now) =>
        new(db, new FixedUtcClock(now), new FastingClockCalculator());

    private static EndFastingEarlyHandler EndEarly(NutritionDbContext db, DateTime now, Mock<IFastingProfessionalNotifier>? notifier = null) =>
        new(db, Reader(db, now), new FastingClockCalculator(),
            (notifier ?? new Mock<IFastingProfessionalNotifier>()).Object, new EndFastingEarlyCommandValidator());

    [Fact]
    public async Task EndEarly_FromAgendaFastingWindow_RecordsEndedEarlyWithRealDurationAndMovesToEating()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        // Eating 12:00-20:00 local. At 08:00 local (11:00 UTC) the fast started at 20:00 the day before (23:00 UTC).
        var now = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc);
        db.FastingAgendas.Add(Agenda());
        await db.SaveChangesAsync();
        var notifier = new Mock<IFastingProfessionalNotifier>();

        var result = await EndEarly(db, now, notifier).HandleAsync(new EndFastingEarlyCommand("  Jantar de família "), User, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingClockCalculator.StatusEating, result.Value!.Clock.Status);
        var row = Assert.Single(db.FastingOverrides);
        Assert.Equal(new DateTime(2026, 6, 14, 23, 0, 0, DateTimeKind.Utc), row.StartedAtUtc);
        Assert.Equal(now, row.EndedEarlyAtUtc);
        Assert.Equal("Jantar de família", row.EndReason);
        notifier.Verify(n => n.NotifyEndedEarlyAsync(User, null, "Jantar de família", It.IsAny<CancellationToken>()), Times.Once);

        var history = await new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator())
            .HandleAsync(new GetFastingHistoryQuery(null, null), User, default);
        var item = Assert.Single(history.Value!.Items);
        Assert.Equal("EndedEarly", item.Outcome);
        Assert.Equal(12 * 3600, item.FastingDurationSeconds);
        Assert.Equal(16, item.PlannedFastHours);
        Assert.Equal(now, item.EndedAtUtc);
        Assert.Equal("Jantar de família", item.EndReason);
    }

    [Fact]
    public async Task EndEarly_FromAgendaWindowAfterEatingEnds_UsesSameDayEatingEndAsStart()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var now = new DateTime(2026, 6, 15, 3, 0, 0, DateTimeKind.Utc); // 03:00 UTC = 00:00 local on 06-15
        db.FastingAgendas.Add(Agenda());
        await db.SaveChangesAsync();

        var result = await EndEarly(db, now).HandleAsync(new EndFastingEarlyCommand(), User, default);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(db.FastingOverrides);
        // Previous eating window ended 20:00 local on 06-14 = 23:00 UTC.
        Assert.Equal(new DateTime(2026, 6, 14, 23, 0, 0, DateTimeKind.Utc), row.StartedAtUtc);
        Assert.Null(row.EndReason);
    }

    [Fact]
    public async Task EndEarly_FromManualOverride_UsesRealElapsedTimeNotPlanned()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var start = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc);
        var now = start.AddHours(11);
        db.FastingAgendas.Add(Agenda());
        var id = Guid.NewGuid();
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = id, UserId = User, Status = FastingOverride.StatusFasting, Protocol = "16:8",
            FastHours = 16, EatHours = 8, StartedAtUtc = start, FastEndsAtUtc = start.AddHours(16)
        });
        await db.SaveChangesAsync();

        var result = await EndEarly(db, now).HandleAsync(new EndFastingEarlyCommand("cansei"), User, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingOverride.StatusEating, db.FastingOverrides.Single(o => o.Id == id).Status);
        var history = await new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator())
            .HandleAsync(new GetFastingHistoryQuery(null, null), User, default);
        var item = Assert.Single(history.Value!.Items);
        Assert.Equal("EndedEarly", item.Outcome);
        Assert.Equal(11 * 3600, item.FastingDurationSeconds);
    }

    [Fact]
    public async Task EndEarly_WhileEatingOrIdle_Returns409WithStableCodeAndWritesNothing()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Agenda());
        await db.SaveChangesAsync();
        // 13:00 local = inside the 12:00-20:00 eating window.
        var eating = await EndEarly(db, Now).HandleAsync(new EndFastingEarlyCommand(), User, default);
        Assert.Equal(409, eating.Error!.StatusCode);
        Assert.Equal("nutrition.fasting.noActiveFast", eating.Error.Code);
        Assert.Empty(db.FastingOverrides);

        await using var empty = FastingTestSupport.CreateDbContext();
        var idle = await EndEarly(empty, Now).HandleAsync(new EndFastingEarlyCommand(), User, default);
        Assert.Equal("nutrition.fasting.noActiveFast", idle.Error!.Code);
    }

    [Fact]
    public async Task EndEarly_WithTooLongReason_Returns400()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var result = await EndEarly(db, Now).HandleAsync(new EndFastingEarlyCommand(new string('x', 281)), User, default);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task EndEarly_WithBlankReason_StoresNull()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Agenda());
        await db.SaveChangesAsync();
        var now = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc);

        await EndEarly(db, now).HandleAsync(new EndFastingEarlyCommand("   "), User, default);

        Assert.Null(db.FastingOverrides.Single().EndReason);
    }

    [Fact]
    public async Task LegacyOverrideEndEarly_RecordsEndedEarlyToo()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var start = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(), UserId = User, Status = FastingOverride.StatusFasting, Protocol = "16:8",
            FastHours = 16, EatHours = 8, StartedAtUtc = start, FastEndsAtUtc = start.AddHours(16)
        });
        await db.SaveChangesAsync();

        var result = await new EndFastingOverrideEarlyHandler(db, new FixedUtcClock(start.AddHours(4)))
            .HandleAsync(new EndFastingOverrideEarlyCommand("x"), User, default);

        Assert.True(result.IsSuccess);
        var row = db.FastingOverrides.Single();
        Assert.Equal(start.AddHours(4), row.EndedEarlyAtUtc);
        Assert.Equal(start.AddHours(4), row.FastEndsAtUtc);
        Assert.Equal("x", row.EndReason);
    }

    [Fact]
    public async Task DeleteAgenda_TurnsFastingOff_KeepsHistoryAndRecommendation()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var agenda = Agenda();
        agenda.RecommendedProtocol = "16:8";
        agenda.RecommendedFastHours = 16;
        agenda.RecommendationStatus = FastingAgenda.RecommendationAdopted;
        db.FastingAgendas.Add(agenda);
        var start = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(), UserId = User, Status = FastingOverride.StatusCompleted, Protocol = "16:8",
            FastHours = 16, EatHours = 8, StartedAtUtc = start.AddDays(-2), FastEndsAtUtc = start.AddDays(-2).AddHours(16),
            CompletedAtUtc = start.AddDays(-1)
        });
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(), UserId = User, Status = FastingOverride.StatusFasting, Protocol = "16:8",
            FastHours = 16, EatHours = 8, StartedAtUtc = start, FastEndsAtUtc = start.AddHours(16)
        });
        await db.SaveChangesAsync();

        var result = await new DeleteFastingAgendaHandler(db, Reader(db, Now), new FixedUtcClock(Now)).HandleAsync(User, default);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Agenda);
        Assert.Null(result.Value.Override);
        Assert.Equal(FastingClockCalculator.StatusIdle, result.Value.Clock.Status);
        Assert.Equal("Adopted", result.Value.Recommendation!.Status);
        Assert.Equal(2, db.FastingOverrides.Count());
        Assert.DoesNotContain(db.FastingOverrides, o => o.Status == FastingOverride.StatusFasting);

        var again = await new DeleteFastingAgendaHandler(db, Reader(db, Now), new FixedUtcClock(Now)).HandleAsync(User, default);
        Assert.True(again.IsSuccess);
    }

    [Fact]
    public async Task DeleteAgenda_WithoutRecommendation_RemovesRow()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Agenda());
        await db.SaveChangesAsync();

        await new DeleteFastingAgendaHandler(db, Reader(db, Now), new FixedUtcClock(Now)).HandleAsync(User, default);

        Assert.Empty(db.FastingAgendas);
    }

    private static DeclineFastingRecommendationHandler Decline(NutritionDbContext db, Mock<IFastingProfessionalNotifier>? notifier = null) =>
        new(db, Reader(db, Now), new FixedUtcClock(Now), (notifier ?? new Mock<IFastingProfessionalNotifier>()).Object,
            new DeclineFastingRecommendationCommandValidator());

    private static FastingAgenda Recommended(string? status) => new()
    {
        UserId = User, RecommendedProtocol = "16:8", RecommendedFastHours = 16, RecommendationStatus = status,
        RecommendedByUserId = 11, RecommendedAtUtc = Now.AddDays(-2), UpdatedAtUtc = Now
    };

    [Fact]
    public async Task Decline_PendingRecommendation_MarksDeclinedAndNotifies()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Recommended(FastingAgenda.RecommendationPending));
        await db.SaveChangesAsync();
        var notifier = new Mock<IFastingProfessionalNotifier>();

        var result = await Decline(db, notifier).HandleAsync(new DeclineFastingRecommendationCommand(" não consigo "), User, default);

        Assert.True(result.IsSuccess);
        var rec = result.Value!.Recommendation!;
        Assert.Equal("Declined", rec.Status);
        Assert.Equal("não consigo", rec.DeclineReason);
        Assert.Equal(Now, rec.DeclinedAtUtc);
        Assert.Equal(11, rec.RecommendedByUserId);
        notifier.Verify(n => n.NotifyRecommendationDeclinedAsync(User, 11, "não consigo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decline_WithoutReason_StoresNull()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Recommended(FastingAgenda.RecommendationPending));
        await db.SaveChangesAsync();

        var result = await Decline(db).HandleAsync(new DeclineFastingRecommendationCommand(""), User, default);

        Assert.Null(result.Value!.Recommendation!.DeclineReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Declined")]
    [InlineData("Adopted")]
    public async Task Decline_WithoutPendingRecommendation_Returns409(string? status)
    {
        await using var db = FastingTestSupport.CreateDbContext();
        if (status is not null)
            db.FastingAgendas.Add(Recommended(status));
        await db.SaveChangesAsync();

        var result = await Decline(db).HandleAsync(new DeclineFastingRecommendationCommand(), User, default);

        Assert.Equal(409, result.Error!.StatusCode);
        Assert.Equal("nutrition.fasting.noPendingRecommendation", result.Error.Code);
    }

    [Fact]
    public async Task Decline_LegacyRecommendationWithSavedAgenda_IsAdoptedSoItCannotBeDeclined()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var legacy = Agenda();
        legacy.RecommendedProtocol = "16:8";
        legacy.RecommendedFastHours = 16;
        db.FastingAgendas.Add(legacy);
        await db.SaveChangesAsync();

        var result = await Decline(db).HandleAsync(new DeclineFastingRecommendationCommand(), User, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task PutAgenda_AfterPendingRecommendation_MarksAdopted_ButNotAfterDecline()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(Recommended(FastingAgenda.RecommendationPending));
        await db.SaveChangesAsync();
        var handler = new PutFastingAgendaHandler(db, new FixedUtcClock(Now), new PutFastingAgendaCommandValidator());
        var cmd = new PutFastingAgendaCommand("16:8", 720, FastingTestSupport.SaoPaulo, null);

        await handler.HandleAsync(cmd, User, default);
        Assert.Equal("Adopted", db.FastingAgendas.Single().RecommendationStatus);

        db.FastingAgendas.Single().RecommendationStatus = FastingAgenda.RecommendationDeclined;
        await db.SaveChangesAsync();
        await handler.HandleAsync(cmd, User, default);
        Assert.Equal("Declined", db.FastingAgendas.Single().RecommendationStatus);
    }

    [Fact]
    public async Task HistoryOrdersEndedEarlyByItsEndAndKeepsPaging()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var t = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 3; i++)
            db.FastingOverrides.Add(new FastingOverride
            {
                Id = Guid.NewGuid(), UserId = User, Status = FastingOverride.StatusCompleted, Protocol = "16:8",
                FastHours = 16, EatHours = 8, StartedAtUtc = t.AddDays(i), FastEndsAtUtc = t.AddDays(i).AddHours(16),
                CompletedAtUtc = t.AddDays(i).AddHours(24)
            });
        var early = new FastingOverride
        {
            Id = Guid.NewGuid(), UserId = User, Status = FastingOverride.StatusEating, Protocol = "16:8",
            FastHours = 16, EatHours = 8, StartedAtUtc = t.AddDays(10), FastEndsAtUtc = t.AddDays(10).AddHours(16)
        };
        early.EndEarly(t.AddDays(10).AddHours(5), "r");
        db.FastingOverrides.Add(early);
        await db.SaveChangesAsync();
        var handler = new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator());

        var first = await handler.HandleAsync(new GetFastingHistoryQuery(null, 2), User, default);
        Assert.Equal(early.Id, first.Value!.Items[0].Id);
        Assert.NotNull(first.Value.NextCursor);
        var second = await handler.HandleAsync(new GetFastingHistoryQuery(first.Value.NextCursor, 2), User, default);
        Assert.Equal(2, second.Value!.Items.Length);
        Assert.DoesNotContain(second.Value.Items, i => i.Id == early.Id);
    }
}

public sealed class FastingModelAndNotifierTests
{
    [Fact]
    public async Task EndEarly_AfterEatingWindowEnds_StartsTheFastAtTodaysEatingEnd()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        // 21:00 local on 06-15 = 00:00 UTC on 06-16; eating ended 20:00 local = 23:00 UTC.
        var now = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
        db.FastingAgendas.Add(new FastingAgenda
        {
            UserId = FastingTestSupport.UserId, Protocol = "16:8", FastHours = 16, EatHours = 8,
            EatingStartMinutes = 720, TimeZone = FastingTestSupport.SaoPaulo, UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
        var handler = new EndFastingEarlyHandler(
            db, new FastingStateReader(db, new FixedUtcClock(now), new FastingClockCalculator()), new FastingClockCalculator(),
            Mock.Of<IFastingProfessionalNotifier>(), new EndFastingEarlyCommandValidator());

        var result = await handler.HandleAsync(new EndFastingEarlyCommand(), FastingTestSupport.UserId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 6, 15, 23, 0, 0, DateTimeKind.Utc), db.FastingOverrides.Single().StartedAtUtc);
    }

    [Fact]
    public void ModelSnapshotMatchesNutritionDbContext_SoTheMigrationIsComplete()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<NutritionDbContext>()
            .UseSqlServer("Server=.;Database=snapshot-check;Trusted_Connection=True;TrustServerCertificate=True").Options;
        using var db = new NutritionDbContext(options);

        Assert.False(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.HasPendingModelChanges(db.Database));
    }

    [Fact]
    public async Task Notifier_NotifiesRecommenderAndNutritionistsOnce_AndNeverFailsTheRequest()
    {
        var sender = new Mock<ShapeUp.Features.Notifications.Shared.Abstractions.IInAppNoticeSender>();
        var relationships = new Mock<ShapeUp.Features.Relationships.Shared.Abstractions.IProfessionalClientRelationshipRepository>();
        relationships.Setup(r => r.ListActiveByClientAsync(22, "Nutrition", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShapeUp.Features.Relationships.Shared.Entities.ProfessionalClientRelationship>
            {
                new() { ProfessionalUserId = 11, ClientUserId = 22, RelationshipType = "Nutrition" },
                new() { ProfessionalUserId = 12, ClientUserId = 22, RelationshipType = "Nutrition" }
            });
        var users = new Mock<ShapeUp.Features.Authorization.Shared.Abstractions.IUserRepository>();
        var notifier = new FastingProfessionalNotifier(
            sender.Object, relationships.Object, users.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FastingProfessionalNotifier>.Instance);

        await notifier.NotifyRecommendationDeclinedAsync(22, 11, "motivo", default);

        sender.Verify(s => s.SendAsync(11, It.IsAny<string>(), It.Is<string>(b => b.Contains("motivo")), It.IsAny<CancellationToken>()), Times.Once);
        sender.Verify(s => s.SendAsync(12, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        sender.Setup(s => s.SendAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));
        await notifier.NotifyEndedEarlyAsync(22, null, null, default);
    }
}
