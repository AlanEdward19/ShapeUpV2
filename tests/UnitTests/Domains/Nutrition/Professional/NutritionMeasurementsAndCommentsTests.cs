using ShapeUp.Features.Nutrition.Comments.AddComment;
using ShapeUp.Features.Nutrition.Comments.GetComments;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.GetMeasurements;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionMeasurementsAndCommentsTests
{
    private const int Pro = 1;
    private const int Client = 2;
    private static readonly DateOnly Day = new(2026, 10, 3);

    [Fact]
    public async Task AddMeasurement_ByLinkedProfessional_StoresForClientRecordedByProfessional()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = new AddMeasurementHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object, new AddMeasurementCommandValidator());

        var result = await handler.HandleAsync(
            new AddMeasurementCommand(Day, WeightKg: 82.5m, HeightCm: 178m, BodyFatPercent: 18m, WaistCm: 90m, HipCm: 100m, Notes: " first visit "),
            Pro, Client, default);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(db.Measurements);
        Assert.Equal(Client, stored.UserId);
        Assert.Equal(Pro, stored.RecordedByUserId);
        Assert.Equal("first visit", stored.Notes);
        Assert.Equal(82.5m, result.Value!.WeightKg);
    }

    [Fact]
    public async Task AddMeasurement_WithoutAccess_Returns403AndStoresNothing()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = new AddMeasurementHandler(db, NutritionProfessionalTestSupport.Policy().Object, new AddMeasurementCommandValidator());

        var result = await handler.HandleAsync(new AddMeasurementCommand(Day, WeightKg: 80m), Pro, Client, default);

        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Empty(db.Measurements);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData(0.5, null, null)]
    [InlineData(80.0, 400.0, null)]
    [InlineData(80.0, null, 95.0)]
    public async Task AddMeasurement_WithInvalidValues_Returns400(double? weight, double? height, double? fat)
    {
        var handler = new AddMeasurementHandler(NutritionProfessionalTestSupport.NewDb(), NutritionProfessionalTestSupport.Policy().Object, new AddMeasurementCommandValidator());

        var result = await handler.HandleAsync(
            new AddMeasurementCommand(Day, (decimal?)weight, (decimal?)height, (decimal?)fat), Client, Client, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task GetMeasurements_ClientReadsOwn_ProfessionalNeedsLink_FiltersByRange()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        db.Measurements.AddRange(
            new NutritionMeasurement { UserId = Client, Date = Day, WeightKg = 80, RecordedByUserId = Pro, CreatedAtUtc = DateTime.UtcNow },
            new NutritionMeasurement { UserId = Client, Date = Day.AddDays(-10), WeightKg = 82, RecordedByUserId = Pro, CreatedAtUtc = DateTime.UtcNow },
            new NutritionMeasurement { UserId = 3, Date = Day, WeightKg = 70, RecordedByUserId = 3, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var linked = new GetMeasurementsHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object);

        var own = await linked.HandleAsync(new GetMeasurementsQuery(), Client, Client, default);
        var range = await linked.HandleAsync(new GetMeasurementsQuery(Day.AddDays(-5), null), Pro, Client, default);
        var other = await linked.HandleAsync(new GetMeasurementsQuery(), Pro, 3, default);

        Assert.Equal([Day, Day.AddDays(-10)], own.Value!.Select(m => m.Date).ToArray());
        Assert.Equal([Day], range.Value!.Select(m => m.Date).ToArray());
        Assert.Equal(403, other.Error!.StatusCode);
    }

    [Fact]
    public async Task AddComment_ByProfessional_StoresWithAuthorName()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = NewAddCommentHandler(db, (Pro, Client));

        var result = await handler.HandleAsync(new AddDiaryCommentCommand(Day, " Good job "), Pro, Client, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Good job", result.Value!.Text);
        Assert.Equal("Dr. Pro", result.Value.AuthorName);
        Assert.Equal(Client, Assert.Single(db.DiaryComments).ClientUserId);
    }

    [Fact]
    public async Task AddComment_OnOwnDiaryOrWithoutLink_Returns403()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = NewAddCommentHandler(db);

        Assert.Equal(403, (await handler.HandleAsync(new AddDiaryCommentCommand(Day, "x"), Client, Client, default)).Error!.StatusCode);
        Assert.Equal(403, (await handler.HandleAsync(new AddDiaryCommentCommand(Day, "x"), Pro, Client, default)).Error!.StatusCode);
        Assert.Empty(db.DiaryComments);
    }

    [Fact]
    public async Task AddComment_OnEntryThatDoesNotBelongToTheDay_Returns404()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var diaryDay = new DiaryDay { UserId = Client, Date = Day };
        diaryDay.Entries.Add(new DiaryEntry
        {
            Id = "entry-1", MealSlot = "lunch", FoodId = "f", QuantityGramsOrMl = 1, ComputedMacros = new MacroValueObject()
        });
        db.DiaryDays.Add(diaryDay);
        await db.SaveChangesAsync();
        var handler = NewAddCommentHandler(db, (Pro, Client));

        var onEntry = await handler.HandleAsync(new AddDiaryCommentCommand(Day, "ok", "entry-1"), Pro, Client, default);
        var wrongDay = await handler.HandleAsync(new AddDiaryCommentCommand(Day.AddDays(1), "ok", "entry-1"), Pro, Client, default);

        Assert.Equal("entry-1", onEntry.Value!.EntryId);
        Assert.Equal(404, wrongDay.Error!.StatusCode);
    }

    [Fact]
    public async Task GetComments_ClientReadsOwnDay_OtherUserGets403_MissingRangeIs400()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        db.DiaryComments.AddRange(
            new NutritionDiaryComment { ClientUserId = Client, Date = Day, AuthorUserId = Pro, Text = "a", CreatedAtUtc = DateTime.UtcNow },
            new NutritionDiaryComment { ClientUserId = Client, Date = Day.AddDays(-1), AuthorUserId = Pro, Text = "b", CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var handler = new GetDiaryCommentsHandler(db, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object,
            NutritionProfessionalTestSupport.Users((Pro, "Dr. Pro")).Object);

        var day = await handler.HandleAsync(new GetDiaryCommentsQuery(Date: Day), Client, Client, default);
        var range = await handler.HandleAsync(new GetDiaryCommentsQuery(From: Day.AddDays(-1), To: Day), Pro, Client, default);
        var denied = await handler.HandleAsync(new GetDiaryCommentsQuery(Date: Day), 9, Client, default);
        var missing = await handler.HandleAsync(new GetDiaryCommentsQuery(), Client, Client, default);

        Assert.Equal(["a"], day.Value!.Select(c => c.Text).ToArray());
        Assert.Equal(["b", "a"], range.Value!.Select(c => c.Text).ToArray());
        Assert.Equal(403, denied.Error!.StatusCode);
        Assert.Equal(400, missing.Error!.StatusCode);
    }

    private static AddDiaryCommentHandler NewAddCommentHandler(
        ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext db, params (int, int)[] allowed) =>
        new(db, NutritionProfessionalTestSupport.Policy(allowed).Object,
            NutritionProfessionalTestSupport.Users((Pro, "Dr. Pro")).Object, new AddDiaryCommentCommandValidator());
}
