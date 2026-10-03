using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.ClientsAdherence;
using ShapeUp.Features.Nutrition.Clients.ListClients;
using ShapeUp.Features.Nutrition.Comments.GetComments;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.GetMeasurements;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionLimitsTests
{
    private const int Pro = 1;
    private const int Client = 2;
    private static readonly DateOnly Day = new(2026, 10, 3);

    private static NutritionMeasurement Measurement(DateOnly date, int id) => new()
    {
        Id = id, UserId = Client, Date = date, WeightKg = 80, RecordedByUserId = Pro, CreatedAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task GetMeasurements_PaginatesNewestFirstWithCursor()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        db.Measurements.AddRange(Measurement(Day, 1), Measurement(Day, 2), Measurement(Day.AddDays(-1), 3), Measurement(Day.AddDays(-2), 4), Measurement(Day.AddDays(-3), 5));
        await db.SaveChangesAsync();
        var handler = new GetMeasurementsHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object);

        var first = await handler.HandleAsync(new GetMeasurementsQuery(PageSize: 2), Pro, Client, default);
        var second = await handler.HandleAsync(new GetMeasurementsQuery(Cursor: first.Value!.NextCursor, PageSize: 2), Pro, Client, default);
        var third = await handler.HandleAsync(new GetMeasurementsQuery(Cursor: second.Value!.NextCursor, PageSize: 2), Pro, Client, default);

        Assert.Equal([2, 1], first.Value.Items.Select(m => m.Id).ToArray());
        Assert.Equal([3, 4], second.Value.Items.Select(m => m.Id).ToArray());
        Assert.Equal([5], third.Value!.Items.Select(m => m.Id).ToArray());
        Assert.Null(third.Value.NextCursor);
    }

    [Fact]
    public async Task GetMeasurements_RangeLongerThanAYear_OrBadCursor_Returns400()
    {
        var handler = new GetMeasurementsHandler(NutritionProfessionalTestSupport.NewDb(), NutritionProfessionalTestSupport.Policy((Pro, Client)).Object);

        var tooLong = await handler.HandleAsync(new GetMeasurementsQuery(Day.AddDays(-366), Day), Pro, Client, default);
        var exactlyAYear = await handler.HandleAsync(new GetMeasurementsQuery(Day.AddDays(-365), Day), Pro, Client, default);
        var badCursor = await handler.HandleAsync(new GetMeasurementsQuery(Cursor: "zzz"), Pro, Client, default);

        Assert.Equal(400, tooLong.Error!.StatusCode);
        Assert.True(exactlyAYear.IsSuccess);
        Assert.Equal(400, badCursor.Error!.StatusCode);
    }

    [Fact]
    public async Task AddMeasurement_FutureDate_Returns400_TodayAndTomorrowOk()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = new AddMeasurementHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object, new AddMeasurementCommandValidator());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var future = await handler.HandleAsync(new AddMeasurementCommand(today.AddDays(2), WeightKg: 80m), Pro, Client, default);
        var tomorrow = await handler.HandleAsync(new AddMeasurementCommand(today.AddDays(1), WeightKg: 80m), Pro, Client, default);

        Assert.Equal(400, future.Error!.StatusCode);
        Assert.True(tomorrow.IsSuccess);
        Assert.Single(db.Measurements);
    }

    [Fact]
    public async Task GetComments_PaginatesOldestFirst_AndLimitsRange()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        for (var i = 1; i <= 5; i++)
            db.DiaryComments.Add(new NutritionDiaryComment
            {
                Id = i, ClientUserId = Client, Date = Day.AddDays(-i), AuthorUserId = Pro, Text = $"c{i}", CreatedAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync();
        var handler = new GetDiaryCommentsHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object,
            NutritionProfessionalTestSupport.Users((Pro, "Dr. Pro")).Object);
        var from = Day.AddDays(-30);

        var first = await handler.HandleAsync(new GetDiaryCommentsQuery(From: from, To: Day, PageSize: 3), Pro, Client, default);
        var second = await handler.HandleAsync(new GetDiaryCommentsQuery(From: from, To: Day, Cursor: first.Value!.NextCursor, PageSize: 3), Pro, Client, default);
        var tooLong = await handler.HandleAsync(new GetDiaryCommentsQuery(From: Day.AddDays(-400), To: Day), Pro, Client, default);

        Assert.Equal(["c5", "c4", "c3"], first.Value.Items.Select(c => c.Text).ToArray());
        Assert.Equal(["c2", "c1"], second.Value!.Items.Select(c => c.Text).ToArray());
        Assert.Null(second.Value.NextCursor);
        Assert.Equal("Dr. Pro", first.Value.Items[0].AuthorName);
        Assert.Equal(400, tooLong.Error!.StatusCode);
    }

    [Fact]
    public async Task ListClients_LooksUpAllNamesWithOneQuery()
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(Pro, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var relationships = new Mock<IProfessionalClientRelationshipRepository>();
        relationships.Setup(r => r.ListActiveByProfessionalAsync(Pro, "Nutrition", default))
            .ReturnsAsync(Enumerable.Range(10, 5).Select(id => new ProfessionalClientRelationship
            {
                ProfessionalUserId = Pro, ClientUserId = id, RelationshipType = "Nutrition", StartedAt = DateTime.UtcNow
            }).ToList());
        var users = NutritionProfessionalTestSupport.Users(Enumerable.Range(10, 5).Select(id => (id, $"C{id}")).ToArray());

        var result = await new ListNutritionClientsHandler(capabilities.Object, relationships.Object, users.Object).HandleAsync(Pro, default);

        Assert.Equal(["C10", "C11", "C12", "C13", "C14"], result.Value!.Select(c => c.Name).ToArray());
        users.Verify(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), default), Times.Once);
        users.Verify(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Adherence_PaginatesClientsWithCursor_AndLooksUpUsersOncePerPage()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var users = NutritionProfessionalTestSupport.Users(Enumerable.Range(10, 5).Select(id => (id, $"C{id}")).ToArray());
        var handler = AdherenceHandler(db, users, Enumerable.Range(10, 5).ToArray());

        var first = await handler.HandleAsync(null, null, 2, Pro, default, Day);
        var second = await handler.HandleAsync(null, first.Value!.NextCursor, 2, Pro, default, Day);
        var third = await handler.HandleAsync(null, second.Value!.NextCursor, 2, Pro, default, Day);
        var bad = await handler.HandleAsync(null, "???", 2, Pro, default, Day);

        Assert.Equal([10, 11], first.Value.Items.Select(i => i.ClientUserId).ToArray());
        Assert.Equal([12, 13], second.Value.Items.Select(i => i.ClientUserId).ToArray());
        Assert.Equal([14], third.Value!.Items.Select(i => i.ClientUserId).ToArray());
        Assert.Null(third.Value.NextCursor);
        Assert.Equal(400, bad.Error!.StatusCode);
        users.Verify(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), default), Times.Exactly(3));
        users.Verify(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Adherence_UsesTheClientsTimeZoneForToday()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        db.FastingAgendas.AddRange(
            new FastingAgenda { UserId = 10, TimeZone = "Pacific/Kiritimati", UpdatedAtUtc = DateTime.UtcNow },
            new FastingAgenda { UserId = 11, TimeZone = "Pacific/Pago_Pago", UpdatedAtUtc = DateTime.UtcNow });
        var localToday10 = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific/Kiritimati")));
        var localToday11 = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific/Pago_Pago")));
        db.DiaryDays.Add(Day10(10, localToday10));
        db.DiaryDays.Add(Day10(11, localToday11));
        await db.SaveChangesAsync();

        var result = await AdherenceHandler(db, NutritionProfessionalTestSupport.Users(), [10, 11]).HandleAsync(1, null, null, Pro, default);

        Assert.All(result.Value!.Items, i => Assert.Equal(1, i.DaysLogged));
    }

    private static DiaryDay Day10(int userId, DateOnly date) => new()
    {
        UserId = userId,
        Date = date,
        Entries =
        [
            new DiaryEntry
            {
                Id = Guid.NewGuid().ToString("N")[..24], MealSlot = "lunch", FoodId = "f", QuantityGramsOrMl = 100,
                ComputedMacros = new MacroValueObject { Kcal = 100, ProteinG = 1, CarbG = 1, FatG = 1 }
            }
        ]
    };

    private static GetClientsAdherenceHandler AdherenceHandler(
        ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext db, Mock<ShapeUp.Features.Authorization.Shared.Abstractions.IUserRepository> users, int[] clientIds)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(Pro, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var relationships = new Mock<IProfessionalClientRelationshipRepository>();
        relationships.Setup(r => r.ListActiveByProfessionalKeysetAsync(Pro, "Nutrition", It.IsAny<int?>(), It.IsAny<int>(), default))
            .ReturnsAsync((int _, string _, int? after, int size, CancellationToken _) => clientIds
                .Select((id, i) => new ProfessionalClientRelationship
                {
                    Id = i + 1, ProfessionalUserId = Pro, ClientUserId = id, RelationshipType = "Nutrition", StartedAt = DateTime.UtcNow
                })
                .Where(r => after == null || r.Id > after)
                .Take(size)
                .ToList());
        return new GetClientsAdherenceHandler(capabilities.Object, relationships.Object, users.Object, db);
    }
}
