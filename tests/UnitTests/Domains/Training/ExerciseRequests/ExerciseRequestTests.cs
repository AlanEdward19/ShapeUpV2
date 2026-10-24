using Microsoft.EntityFrameworkCore;
using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Authorization.Resolver;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.ExerciseRequests.ApproveExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.CreateExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.GetExerciseRequestById;
using ShapeUp.Features.Training.ExerciseRequests.ListExerciseRequests;
using ShapeUp.Features.Training.ExerciseRequests.RejectExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.ExerciseRequests;

public sealed class ExerciseRequestTests
{
    private const int Requester = 5;
    private const int Admin = 1;

    private readonly TrainingDbContext _db = new(new DbContextOptionsBuilder<TrainingDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly Mock<IEquipmentRepository> _equipments = new();
    private readonly Mock<IExerciseRequestSideEffects> _effects = new();
    private readonly Mock<IUserPlatformRoleRepository> _roles = new();
    private readonly Mock<IProfessionalCapabilityService> _capabilities = new();
    private readonly Mock<ICapabilityResolver> _resolver = new();

    public ExerciseRequestTests()
    {
        _equipments.Setup(e => e.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Equipment { Id = 3, Name = "Machine", NamePt = "Máquina" });
        _roles.Setup(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPlatformRole>());
        _capabilities.Setup(c => c.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalCapabilitiesResponse(false, false));
        _resolver.Setup(r => r.ResolveAsync(Admin, "platform.exercises.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CapabilityResult.Allow());
        _resolver.Setup(r => r.ResolveAsync(It.Is<int>(u => u != Admin), It.IsAny<string>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CapabilityResult.Deny("no"));
    }

    private ExerciseRequestRepository Repo() => new(_db);

    private ExerciseRequestResponseBuilder Builder()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                new() { Id = Requester, FirebaseUid = "f5", Email = "r@x.com", DisplayName = "Rita" },
                new() { Id = Admin, FirebaseUid = "f1", Email = "a@x.com", DisplayName = "Admin" }
            });
        return new ExerciseRequestResponseBuilder(_equipments.Object, users.Object);
    }

    private CreateExerciseRequestHandler Create() => new(
        Repo(), _equipments.Object, _roles.Object, _capabilities.Object, Builder(), _effects.Object,
        new CreateExerciseRequestCommandValidator());

    private ApproveExerciseRequestHandler Approve() => new(
        Repo(), _equipments.Object, new CreateExerciseCommandValidator(), new ApproveExerciseRequestCommandValidator(),
        Builder(), _effects.Object);

    private RejectExerciseRequestHandler Reject() => new(
        Repo(), Builder(), _effects.Object, new RejectExerciseRequestCommandValidator());

    private static CreateExerciseRequestCommand Valid(string name = "Paused Hack Squat") => new(
        name, "weight", ExerciseType.WeightBased, ["Quadriceps", "glutes"], [3, 3], " Smith ", " notes ", "https://example.com/x");

    private static ApproveExerciseRequestBody Form(string name = "Paused Hack Squat") => new(
        name, "Agachamento hack com pausa", "desc", null,
        [new ExerciseMuscleDto(MuscleGroup.Quadriceps, 100), new ExerciseMuscleDto(MuscleGroup.Glutes, 40)],
        [3], [new ExerciseStepDto("Descend")], ExerciseType.WeightBased, "Merged #14");

    private async Task<int> SeedPending(int userId = Requester)
    {
        var r = await Create().HandleAsync(Valid(), userId, default);
        Assert.True(r.IsSuccess, r.Error?.Message);
        return r.Value!.Id;
    }

    [Fact]
    public async Task Create_StoresPendingRequestFromTokenUser_NormalizedAndAudited()
    {
        var result = await Create().HandleAsync(Valid(), Requester, default);

        Assert.True(result.IsSuccess);
        var r = result.Value!;
        Assert.Equal("Pending", r.Status);
        Assert.Equal(Requester, r.RequestedByUserId);
        Assert.Equal("Rita", r.RequestedByName);
        Assert.Equal("IndependentClient", r.RequestedByRole);
        Assert.Equal(["Quadriceps", "Glutes"], r.MuscleGroups);
        Assert.Equal([3], r.EquipmentIds);
        Assert.Equal(["Machine"], r.EquipmentNames);
        Assert.Equal("Smith", r.EquipmentOther);
        Assert.Equal("notes", r.Notes);
        Assert.Null(r.ReviewedByUserId);
        _effects.Verify(e => e.AuditAsync("created", Requester, r.Id, null, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ResolvesRoleFromCapabilitiesAndPlatformRoles()
    {
        _capabilities.Setup(c => c.GetAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        Assert.Equal("Nutritionist", (await Create().HandleAsync(Valid(), 7, default)).Value!.RequestedByRole);

        _roles.Setup(r => r.GetByUserIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPlatformRole> { new() { UserId = 8, Role = PlatformRoleType.GymClient } });
        Assert.Equal("GymClient", (await Create().HandleAsync(Valid(), 8, default)).Value!.RequestedByRole);

        _roles.Setup(r => r.GetByUserIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserPlatformRole> { new() { UserId = 9, Role = PlatformRoleType.Trainer } });
        Assert.Equal("Trainer", (await Create().HandleAsync(Valid(), 9, default)).Value!.RequestedByRole);
    }

    [Theory]
    [InlineData("", "weight", ExerciseType.WeightBased, new[] { "Chest" }, null, null)]
    [InlineData("x", "yoga", ExerciseType.TimeBased, new[] { "Chest" }, null, null)]
    [InlineData("x", "weight", ExerciseType.TimeBased, new[] { "Chest" }, null, null)]
    [InlineData("x", "stretch", ExerciseType.WeightBased, new[] { "Chest" }, null, null)]
    [InlineData("x", "weight", ExerciseType.WeightBased, new string[0], null, null)]
    [InlineData("x", "weight", ExerciseType.WeightBased, new[] { "Pinky" }, null, null)]
    [InlineData("x", "weight", ExerciseType.WeightBased, new[] { "Chest" }, "ftp://x.com", null)]
    [InlineData("x", "weight", ExerciseType.WeightBased, new[] { "Chest" }, "javascript:alert(1)", null)]
    [InlineData("x", "weight", ExerciseType.WeightBased, new[] { "Chest" }, null, "tooLongNotes")]
    public async Task Create_InvalidInput_Returns400AndStoresNothing(
        string name, string kind, ExerciseType type, string[] groups, string? url, string? notes)
    {
        var notesValue = notes == "tooLongNotes" ? new string('n', 1001) : notes;

        var result = await Create().HandleAsync(
            new CreateExerciseRequestCommand(name, kind, type, groups, [], null, notesValue, url), Requester, default);

        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Empty(_db.ExerciseRequests);
    }

    [Fact]
    public async Task Create_CardioMayHaveNoMuscles_AndNameOver160IsRejected()
    {
        var cardio = new CreateExerciseRequestCommand("Rowing", "cardio", ExerciseType.TimeBased, [], null, null, null, null);
        Assert.True((await Create().HandleAsync(cardio, Requester, default)).IsSuccess);

        var longName = Valid(new string('a', 161));
        Assert.Equal(400, (await Create().HandleAsync(longName, Requester, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownEquipment_Returns404_AndTenPendingIsTheLimit()
    {
        var unknown = Valid() with { EquipmentIds = [99] };
        Assert.Equal(404, (await Create().HandleAsync(unknown, Requester, default)).Error!.StatusCode);

        for (var i = 0; i < 10; i++)
            await SeedPending();
        var eleventh = await Create().HandleAsync(Valid(), Requester, default);

        Assert.Equal(429, eleventh.Error!.StatusCode);
        Assert.Equal("training.exerciseRequest.pendingLimit", eleventh.Error.Code);
        // Another user is not affected.
        Assert.True((await Create().HandleAsync(Valid(), 6, default)).IsSuccess);
    }

    [Fact]
    public async Task Approve_CreatesExerciseAndLinksBothWaysInOneCall_AndNotifies()
    {
        var id = await SeedPending();

        var result = await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form()), Admin, default);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var exercise = await _db.Exercises.Include(e => e.MuscleProfiles).Include(e => e.Steps)
            .Include(e => e.ExerciseEquipments).SingleAsync();
        Assert.Equal(id, exercise.ExerciseRequestId);
        Assert.Equal(2, exercise.MuscleProfiles.Count);
        Assert.Single(exercise.Steps);
        Assert.Single(exercise.ExerciseEquipments);
        var request = await _db.ExerciseRequests.SingleAsync();
        Assert.Equal("Approved", request.Status);
        Assert.Equal(exercise.Id, request.ApprovedExerciseId);
        Assert.Equal(Admin, request.ReviewedByUserId);
        Assert.NotNull(request.ReviewedAtUtc);
        Assert.Equal(exercise.Id, result.Value!.ApprovedExerciseId);
        Assert.Equal("Paused Hack Squat", result.Value.ExerciseName);
        Assert.Equal("Merged #14", result.Value.AdminNote);
        _effects.Verify(e => e.AuditAsync("approved", Admin, id, exercise.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _effects.Verify(e => e.NotifyApprovedAsync(Requester, "Paused Hack Squat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_Twice_Returns409AndCreatesOnlyOneExercise()
    {
        var id = await SeedPending();
        await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form()), Admin, default);

        var second = await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form("Other")), Admin, default);

        Assert.Equal(409, second.Error!.StatusCode);
        Assert.Equal("training.exerciseRequest.alreadyReviewed", second.Error.Code);
        Assert.Single(_db.Exercises);
    }

    [Fact]
    public async Task Approve_InvalidFormOrUnknownEquipment_CreatesNothingAndKeepsRequestPending()
    {
        var id = await SeedPending();

        var noName = await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form("")), Admin, default);
        var badEquip = await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form() with { EquipmentIds = [77] }), Admin, default);
        var longNote = await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form() with { AdminNote = new string('x', 501) }), Admin, default);

        Assert.Equal(400, noName.Error!.StatusCode);
        Assert.Equal(404, badEquip.Error!.StatusCode);
        Assert.Equal(400, longNote.Error!.StatusCode);
        Assert.Empty(_db.Exercises);
        Assert.Equal("Pending", (await _db.ExerciseRequests.SingleAsync()).Status);
        _effects.Verify(e => e.NotifyApprovedAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Approve_UnknownRequest_Returns404()
    {
        var result = await Approve().HandleAsync(new ApproveExerciseRequestCommand(999, Form()), Admin, default);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Reject_RequiresReason_StoresItAndNotifies()
    {
        var id = await SeedPending();

        var blank = await Reject().HandleAsync(new RejectExerciseRequestCommand(id, "  "), Admin, default);
        var tooLong = await Reject().HandleAsync(new RejectExerciseRequestCommand(id, new string('r', 281)), Admin, default);
        Assert.Equal(400, blank.Error!.StatusCode);
        Assert.Equal(400, tooLong.Error!.StatusCode);
        Assert.Equal("Pending", (await _db.ExerciseRequests.SingleAsync()).Status);

        var ok = await Reject().HandleAsync(new RejectExerciseRequestCommand(id, " duplicado "), Admin, default);

        Assert.True(ok.IsSuccess);
        Assert.Equal("Rejected", ok.Value!.Status);
        Assert.Equal("duplicado", ok.Value.RejectionReason);
        Assert.Equal(Admin, ok.Value.ReviewedByUserId);
        _effects.Verify(e => e.NotifyRejectedAsync(Requester, "Paused Hack Squat", "duplicado", It.IsAny<CancellationToken>()), Times.Once);
        _effects.Verify(e => e.AuditAsync("rejected", Admin, id, null, "duplicado", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_AfterReview_Returns409_AndReviewedRequestsCannotBeApproved()
    {
        var id = await SeedPending();
        await Reject().HandleAsync(new RejectExerciseRequestCommand(id, "no"), Admin, default);

        Assert.Equal(409, (await Reject().HandleAsync(new RejectExerciseRequestCommand(id, "again"), Admin, default)).Error!.StatusCode);
        Assert.Equal(409, (await Approve().HandleAsync(new ApproveExerciseRequestCommand(id, Form()), Admin, default)).Error!.StatusCode);
        Assert.Empty(_db.Exercises);
    }

    [Fact]
    public async Task GetById_OwnerAndAdminCanRead_OthersGet404_RequesterNeverSeesReviewer()
    {
        var id = await SeedPending();
        await Reject().HandleAsync(new RejectExerciseRequestCommand(id, "no"), Admin, default);
        var handler = new GetExerciseRequestByIdHandler(Repo(), _resolver.Object, Builder());

        var owner = await handler.HandleAsync(new GetExerciseRequestByIdQuery(id), Requester, default);
        var admin = await handler.HandleAsync(new GetExerciseRequestByIdQuery(id), Admin, default);
        var stranger = await handler.HandleAsync(new GetExerciseRequestByIdQuery(id), 42, default);

        Assert.True(owner.IsSuccess);
        Assert.Null(owner.Value!.ReviewedByUserId);
        Assert.Null(owner.Value.ReviewedByName);
        Assert.Equal("no", owner.Value.RejectionReason);
        Assert.Equal(Admin, admin.Value!.ReviewedByUserId);
        Assert.Equal("Admin", admin.Value.ReviewedByName);
        Assert.Equal(404, stranger.Error!.StatusCode);
    }

    [Fact]
    public async Task List_MineIsOwnNewestFirstKeysetPaged_AdminListFiltersByStatus()
    {
        var ids = new List<int>();
        for (var i = 0; i < 3; i++)
            ids.Add(await SeedPending());
        await SeedPending(userId: 6);
        await Reject().HandleAsync(new RejectExerciseRequestCommand(ids[0], "no"), Admin, default);
        var handler = new ListExerciseRequestsHandler(Repo(), Builder(), new ListExerciseRequestsQueryValidator());

        var first = await handler.HandleAsync(new ListExerciseRequestsQuery(null, null, 2), Requester, true, default);
        Assert.Equal([ids[2], ids[1]], first.Value!.Items.Select(i => i.Id));
        Assert.NotNull(first.Value.NextCursor);
        var second = await handler.HandleAsync(new ListExerciseRequestsQuery(null, first.Value.NextCursor, 2), Requester, true, default);
        Assert.Equal([ids[0]], second.Value!.Items.Select(i => i.Id));
        Assert.Null(second.Value.NextCursor);

        var pending = await handler.HandleAsync(new ListExerciseRequestsQuery("pending", null, 50), Admin, false, default);
        Assert.Equal(3, pending.Value!.Items.Length);
        Assert.All(pending.Value.Items, i => Assert.Equal("Pending", i.Status));

        Assert.Equal(400, (await handler.HandleAsync(new ListExerciseRequestsQuery("bogus", null, null), Admin, false, default)).Error!.StatusCode);
        Assert.Equal(400, (await handler.HandleAsync(new ListExerciseRequestsQuery(null, "###", null), Admin, false, default)).Error!.StatusCode);
    }

    [Fact]
    public void ModelSnapshotMatchesTrainingDbContext_SoTheMigrationIsComplete()
    {
        var options = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseSqlServer("Server=.;Database=snapshot-check;Trusted_Connection=True;TrustServerCertificate=True").Options;
        using var db = new TrainingDbContext(options);

        Assert.False(RelationalDatabaseFacadeExtensions.HasPendingModelChanges(db.Database));
    }
}
