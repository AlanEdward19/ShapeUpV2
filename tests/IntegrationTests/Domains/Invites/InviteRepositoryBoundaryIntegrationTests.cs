namespace IntegrationTests.Domains.Invites;

using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

/// <summary>
/// The conditional UPDATEs are what decide who wins a race and where a deadline falls, so they are checked against the
/// real database: on the exact tick of the deadline, one tick on each side, and with many callers at once.
/// </summary>
[Collection("SQL Server Write Operations")]
public sealed class InviteRepositoryBoundaryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    private IntegrationWebApplicationFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private static int NewOwner() => Random.Shared.Next(1_000_000, 2_000_000_000);

    private async Task<int> SeedNutritionAsync(int professional, DateTime expires,
        ProfessionalClientInviteStatus status = ProfessionalClientInviteStatus.Pending, string type = "Nutrition")
    {
        await using var context = fixture.CreateRelationshipsDbContext();
        var invite = new ProfessionalClientInvite
        {
            ProfessionalUserId = professional,
            RelationshipType = type,
            TokenHash = Guid.NewGuid().ToString("N"),
            Status = status,
            CreatedAtUtc = Now.AddDays(-1),
            ExpiresAtUtc = expires
        };
        context.Set<ProfessionalClientInvite>().Add(invite);
        await context.SaveChangesAsync();
        return invite.Id;
    }

    private async Task<ProfessionalClientInvite> ReadNutritionAsync(int id)
    {
        await using var context = fixture.CreateRelationshipsDbContext();
        return await context.Set<ProfessionalClientInvite>().AsNoTracking().SingleAsync(i => i.Id == id);
    }

    private async Task<int> SeedTrainerAsync(int trainer, DateTime expires,
        TrainerClientInviteStatus status = TrainerClientInviteStatus.Invited)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        var invite = new TrainerClientInvite
        {
            TrainerId = trainer,
            InviteeEmail = $"{Guid.NewGuid():N}@boundary.test",
            AccessTokenHash = Guid.NewGuid().ToString("N"),
            Status = status,
            ExpiresAtUtc = expires
        };
        context.TrainerClientInvites.Add(invite);
        await context.SaveChangesAsync();
        return invite.Id;
    }

    private async Task<TrainerClientInvite> ReadTrainerAsync(int id)
    {
        await using var context = fixture.CreateGymManagementDbContext();
        return await context.TrainerClientInvites.AsNoTracking().SingleAsync(i => i.Id == id);
    }

    private T Repo<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    // ---- nutrition invites: accept ------------------------------------------------------------------------------

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Nutrition_TryAccept_FollowsTheExclusiveDeadline(long ticksFromNow, bool accepted)
    {
        var id = await SeedNutritionAsync(NewOwner(), Now.AddTicks(ticksFromNow));
        using var scope = _factory.Services.CreateScope();

        var result = await Repo<IProfessionalClientInviteRepository>(scope).TryAcceptAsync(id, 77, Now, default);

        var stored = await ReadNutritionAsync(id);
        Assert.Equal(accepted, result);
        Assert.Equal(accepted ? ProfessionalClientInviteStatus.Accepted : ProfessionalClientInviteStatus.Pending, stored.Status);
        Assert.Equal(accepted ? 77 : null, stored.AcceptedByUserId);
        Assert.Equal(accepted ? Now : null, stored.AcceptedAtUtc);
    }

    [Theory]
    [InlineData(ProfessionalClientInviteStatus.Accepted)]
    [InlineData(ProfessionalClientInviteStatus.Revoked)]
    public async Task Nutrition_TryAccept_NeverTakesAnInviteThatIsNotPending(ProfessionalClientInviteStatus status)
    {
        var id = await SeedNutritionAsync(NewOwner(), Now.AddDays(1), status);
        using var scope = _factory.Services.CreateScope();

        Assert.False(await Repo<IProfessionalClientInviteRepository>(scope).TryAcceptAsync(id, 77, Now, default));
        Assert.Equal(status, (await ReadNutritionAsync(id)).Status);
    }

    [Fact]
    public async Task Nutrition_TryAccept_WithManyCallersAtOnce_ExactlyOneWins()
    {
        var id = await SeedNutritionAsync(NewOwner(), Now.AddDays(1));

        var results = await Task.WhenAll(Enumerable.Range(1, 12).Select(async client =>
        {
            using var scope = _factory.Services.CreateScope();
            return await Repo<IProfessionalClientInviteRepository>(scope).TryAcceptAsync(id, client, Now, default);
        }));

        Assert.Single(results, won => won);
        Assert.Equal(ProfessionalClientInviteStatus.Accepted, (await ReadNutritionAsync(id)).Status);
    }

    [Fact]
    public async Task Nutrition_AcceptAndRevokeAtOnce_ExactlyOneWins()
    {
        for (var round = 0; round < 10; round++)
        {
            var owner = NewOwner();
            var id = await SeedNutritionAsync(owner, Now.AddDays(1));
            var gate = new TaskCompletionSource();
            var accept = Task.Run(async () =>
            {
                await gate.Task;
                using var scope = _factory.Services.CreateScope();
                return await Repo<IProfessionalClientInviteRepository>(scope).TryAcceptAsync(id, 77, Now, default);
            });
            var revoke = Task.Run(async () =>
            {
                await gate.Task;
                using var scope = _factory.Services.CreateScope();
                return await Repo<IProfessionalClientInviteRepository>(scope).TryRevokeAsync(id, owner, default);
            });
            gate.SetResult();
            var (accepted, revoked) = (await accept, await revoke);

            Assert.NotEqual(accepted, revoked);
            Assert.Equal(accepted ? ProfessionalClientInviteStatus.Accepted : ProfessionalClientInviteStatus.Revoked,
                (await ReadNutritionAsync(id)).Status);
        }
    }

    // ---- nutrition invites: revoke, release, count, list -------------------------------------------------------

    [Fact]
    public async Task Nutrition_TryRevoke_OnlyForTheOwnerAndOnlyWhilePending()
    {
        var owner = NewOwner();
        var pending = await SeedNutritionAsync(owner, Now.AddDays(1));
        var accepted = await SeedNutritionAsync(owner, Now.AddDays(1), ProfessionalClientInviteStatus.Accepted);
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<IProfessionalClientInviteRepository>(scope);

        Assert.False(await repo.TryRevokeAsync(pending, owner + 1, default));
        Assert.Equal(ProfessionalClientInviteStatus.Pending, (await ReadNutritionAsync(pending)).Status);
        Assert.False(await repo.TryRevokeAsync(accepted, owner, default));
        Assert.True(await repo.TryRevokeAsync(pending, owner, default));
        Assert.False(await repo.TryRevokeAsync(pending, owner, default));
        Assert.Equal(ProfessionalClientInviteStatus.Revoked, (await ReadNutritionAsync(pending)).Status);
    }

    [Fact]
    public async Task Nutrition_ReleaseAccepted_OnlyGivesBackTheInviteOfThatClient()
    {
        var id = await SeedNutritionAsync(NewOwner(), Now.AddDays(1));
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<IProfessionalClientInviteRepository>(scope);
        Assert.True(await repo.TryAcceptAsync(id, 77, Now, default));

        Assert.False(await repo.ReleaseAcceptedAsync(id, 78, default));
        Assert.Equal(ProfessionalClientInviteStatus.Accepted, (await ReadNutritionAsync(id)).Status);
        Assert.True(await repo.ReleaseAcceptedAsync(id, 77, default));

        var back = await ReadNutritionAsync(id);
        Assert.Equal(ProfessionalClientInviteStatus.Pending, back.Status);
        Assert.Null(back.AcceptedByUserId);
        Assert.Null(back.AcceptedAtUtc);
    }

    [Fact]
    public async Task Nutrition_CountPending_ExcludesTheDeadlineTickAndEveryOtherStatus()
    {
        var owner = NewOwner();
        await SeedNutritionAsync(owner, Now.AddTicks(-1));
        await SeedNutritionAsync(owner, Now);
        await SeedNutritionAsync(owner, Now.AddTicks(1));
        await SeedNutritionAsync(owner, Now.AddDays(3), ProfessionalClientInviteStatus.Revoked);
        await SeedNutritionAsync(owner, Now.AddDays(3), ProfessionalClientInviteStatus.Accepted);
        await SeedNutritionAsync(owner, Now.AddDays(3), type: "Training");
        await SeedNutritionAsync(owner + 1, Now.AddDays(3));
        using var scope = _factory.Services.CreateScope();

        var count = await Repo<IProfessionalClientInviteRepository>(scope).CountPendingAsync(owner, "Nutrition", Now, default);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Nutrition_List_KeysetIsExclusiveNewestFirstAndKeepsToItsOwner()
    {
        var owner = NewOwner();
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
            ids.Add(await SeedNutritionAsync(owner, Now.AddDays(1), (ProfessionalClientInviteStatus)(i % 3)));
        await SeedNutritionAsync(owner + 1, Now.AddDays(1));
        await SeedNutritionAsync(owner, Now.AddDays(1), type: "Training");
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<IProfessionalClientInviteRepository>(scope);

        var all = await repo.ListAsync(owner, "Nutrition", null, 100, default);
        Assert.Equal(ids.AsEnumerable().Reverse(), all.Select(i => i.Id));

        // The cursor is the last id already seen: it never comes back, and the next one does.
        var afterNewest = await repo.ListAsync(owner, "Nutrition", ids[^1], 100, default);
        Assert.Equal(ids.Take(4).Reverse(), afterNewest.Select(i => i.Id));
        Assert.Empty(await repo.ListAsync(owner, "Nutrition", ids[0], 100, default));

        // A page of exactly N rows, and of N - 1.
        Assert.Equal(5, (await repo.ListAsync(owner, "Nutrition", null, 5, default)).Count);
        Assert.Equal(4, (await repo.ListAsync(owner, "Nutrition", null, 4, default)).Count);
    }

    // ---- trainer invites ----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Trainer_TryAccept_FollowsTheExclusiveDeadline(long ticksFromNow, bool accepted)
    {
        var id = await SeedTrainerAsync(NewOwner(), Now.AddTicks(ticksFromNow));
        using var scope = _factory.Services.CreateScope();

        var result = await Repo<ITrainerClientInviteRepository>(scope).TryAcceptAsync(id, 88, Now, default);

        var stored = await ReadTrainerAsync(id);
        Assert.Equal(accepted, result);
        Assert.Equal(accepted ? TrainerClientInviteStatus.Accepted : TrainerClientInviteStatus.Invited, stored.Status);
        Assert.Equal(accepted ? 88 : null, stored.AcceptedByUserId);
        Assert.Equal(accepted ? Now : null, stored.AcceptedAtUtc);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public async Task Trainer_TryRevoke_FollowsTheExclusiveDeadline(long ticksFromNow, bool revoked)
    {
        var trainer = NewOwner();
        var id = await SeedTrainerAsync(trainer, Now.AddTicks(ticksFromNow));
        using var scope = _factory.Services.CreateScope();

        var result = await Repo<ITrainerClientInviteRepository>(scope).TryRevokeAsync(id, trainer, Now, default);

        Assert.Equal(revoked, result);
        Assert.Equal(revoked ? TrainerClientInviteStatus.Revoked : TrainerClientInviteStatus.Invited, (await ReadTrainerAsync(id)).Status);
    }

    [Theory]
    [InlineData(TrainerClientInviteStatus.Accepted)]
    [InlineData(TrainerClientInviteStatus.Revoked)]
    [InlineData(TrainerClientInviteStatus.Superseded)]
    [InlineData(TrainerClientInviteStatus.Expired)]
    public async Task Trainer_TryAcceptAndTryRevoke_NeverTouchAnInviteThatIsNotInvited(TrainerClientInviteStatus status)
    {
        var trainer = NewOwner();
        var id = await SeedTrainerAsync(trainer, Now.AddDays(1), status);
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<ITrainerClientInviteRepository>(scope);

        Assert.False(await repo.TryAcceptAsync(id, 88, Now, default));
        Assert.False(await repo.TryRevokeAsync(id, trainer, Now, default));
        Assert.Equal(status, (await ReadTrainerAsync(id)).Status);
    }

    [Fact]
    public async Task Trainer_TryRevoke_NeverRevokesAnotherTrainersInvite()
    {
        var trainer = NewOwner();
        var id = await SeedTrainerAsync(trainer, Now.AddDays(1));
        using var scope = _factory.Services.CreateScope();

        Assert.False(await Repo<ITrainerClientInviteRepository>(scope).TryRevokeAsync(id, trainer + 1, Now, default));
        Assert.Equal(TrainerClientInviteStatus.Invited, (await ReadTrainerAsync(id)).Status);
    }

    [Fact]
    public async Task Trainer_ReleaseAccepted_OnlyGivesBackTheInviteOfThatClient()
    {
        var id = await SeedTrainerAsync(NewOwner(), Now.AddDays(1));
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<ITrainerClientInviteRepository>(scope);
        Assert.True(await repo.TryAcceptAsync(id, 88, Now, default));

        Assert.False(await repo.ReleaseAcceptedAsync(id, 89, default));
        Assert.True(await repo.ReleaseAcceptedAsync(id, 88, default));

        var back = await ReadTrainerAsync(id);
        Assert.Equal(TrainerClientInviteStatus.Invited, back.Status);
        Assert.Null(back.AcceptedByUserId);
        Assert.Null(back.AcceptedAtUtc);
    }

    [Fact]
    public async Task Trainer_TryAccept_WithManyCallersAtOnce_ExactlyOneWins()
    {
        var id = await SeedTrainerAsync(NewOwner(), Now.AddDays(1));

        var results = await Task.WhenAll(Enumerable.Range(1, 12).Select(async client =>
        {
            using var scope = _factory.Services.CreateScope();
            return await Repo<ITrainerClientInviteRepository>(scope).TryAcceptAsync(id, client, Now, default);
        }));

        Assert.Single(results, won => won);
        Assert.Equal(TrainerClientInviteStatus.Accepted, (await ReadTrainerAsync(id)).Status);
    }

    [Fact]
    public async Task Trainer_AcceptAndRevokeAtOnce_ExactlyOneWins()
    {
        for (var round = 0; round < 10; round++)
        {
            var trainer = NewOwner();
            var id = await SeedTrainerAsync(trainer, Now.AddDays(1));
            var gate = new TaskCompletionSource();
            var accept = Task.Run(async () =>
            {
                await gate.Task;
                using var scope = _factory.Services.CreateScope();
                return await Repo<ITrainerClientInviteRepository>(scope).TryAcceptAsync(id, 88, Now, default);
            });
            var revoke = Task.Run(async () =>
            {
                await gate.Task;
                using var scope = _factory.Services.CreateScope();
                return await Repo<ITrainerClientInviteRepository>(scope).TryRevokeAsync(id, trainer, Now, default);
            });
            gate.SetResult();
            var (accepted, revoked) = (await accept, await revoke);

            Assert.NotEqual(accepted, revoked);
            Assert.Equal(accepted ? TrainerClientInviteStatus.Accepted : TrainerClientInviteStatus.Revoked,
                (await ReadTrainerAsync(id)).Status);
        }
    }

    [Fact]
    public async Task Trainer_CountWaiting_MatchesTheNutritionistsCount_ExcludesTheDeadlineTickAndTheExcludedInvite()
    {
        var trainer = NewOwner();
        await SeedTrainerAsync(trainer, Now.AddTicks(-1));
        await SeedTrainerAsync(trainer, Now);
        var alive = await SeedTrainerAsync(trainer, Now.AddTicks(1));
        var other = await SeedTrainerAsync(trainer, Now.AddDays(3));
        await SeedTrainerAsync(trainer, Now.AddDays(3), TrainerClientInviteStatus.Revoked);
        await SeedTrainerAsync(trainer, Now.AddDays(3), TrainerClientInviteStatus.Accepted);
        await SeedTrainerAsync(trainer, Now.AddDays(3), TrainerClientInviteStatus.Superseded);
        await SeedTrainerAsync(trainer + 1, Now.AddDays(3));
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<ITrainerClientInviteRepository>(scope);

        Assert.Equal(2, await repo.CountWaitingAsync(trainer, Now, null, default));
        Assert.Equal(1, await repo.CountWaitingAsync(trainer, Now, alive, default));
        Assert.Equal(1, await repo.CountWaitingAsync(trainer, Now, other, default));
    }

    [Fact]
    public async Task Trainer_List_IsNewestFirstByIdAndBoundedByTheCursor()
    {
        var trainer = NewOwner();
        var ids = new List<int>();
        for (var i = 0; i < 4; i++) ids.Add(await SeedTrainerAsync(trainer, Now.AddDays(1)));
        await SeedTrainerAsync(trainer + 1, Now.AddDays(1));
        using var scope = _factory.Services.CreateScope();
        var repo = Repo<ITrainerClientInviteRepository>(scope);

        var all = await repo.ListAsync(trainer, null, 10, default);
        var below = await repo.ListAsync(trainer, ids[2], 10, default);
        var limited = await repo.ListAsync(trainer, null, 2, default);

        Assert.Equal(ids.AsEnumerable().Reverse(), all.Select(i => i.Id));
        Assert.Equal([ids[1], ids[0]], below.Select(i => i.Id));
        Assert.Equal([ids[3], ids[2]], limited.Select(i => i.Id));
    }
}
