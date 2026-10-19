using Moq;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Comments.ManageComment;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.ManageMeasurement;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionEditDeleteTests
{
    private const int Pro = 1;
    private const int Client = 2;
    private static readonly DateOnly Day = new(2026, 10, 3);

    private static async Task<(ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext Db, int MeasurementId, int OwnMeasurementId, int CommentId)> Seed()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var byPro = new NutritionMeasurement { UserId = Client, Date = Day, WeightKg = 80, RecordedByUserId = Pro, CreatedAtUtc = DateTime.UtcNow };
        var byClient = new NutritionMeasurement { UserId = Client, Date = Day, WeightKg = 81, RecordedByUserId = Client, CreatedAtUtc = DateTime.UtcNow };
        var comment = new NutritionDiaryComment { ClientUserId = Client, Date = Day, AuthorUserId = Pro, Text = "ok", CreatedAtUtc = DateTime.UtcNow };
        db.Measurements.AddRange(byPro, byClient);
        db.DiaryComments.Add(comment);
        await db.SaveChangesAsync();
        return (db, byPro.Id, byClient.Id, comment.Id);
    }

    private static ManageMeasurementHandler Measurements(ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext db, bool linked = true) =>
        new(db, (linked ? NutritionProfessionalTestSupport.Policy((Pro, Client)) : NutritionProfessionalTestSupport.Policy()).Object,
            new AddMeasurementCommandValidator());

    private static ManageDiaryCommentHandler Comments(ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext db, bool linked = true)
    {
        var users = new Mock<IUserRepository>();
        return new ManageDiaryCommentHandler(
            db, (linked ? NutritionProfessionalTestSupport.Policy((Pro, Client)) : NutritionProfessionalTestSupport.Policy()).Object,
            users.Object, new EditDiaryCommentCommandValidator());
    }

    [Fact]
    public async Task EditMeasurement_ByRecorder_ChangesValues()
    {
        var (db, id, _, _) = await Seed();

        var result = await Measurements(db).EditAsync(id, new AddMeasurementCommand(Day, WeightKg: 79m, Notes: " revisado "), Pro, Client, default);

        Assert.True(result.IsSuccess);
        var stored = db.Measurements.Single(m => m.Id == id);
        Assert.Equal(79m, stored.WeightKg);
        Assert.Equal("revisado", stored.Notes);
        Assert.Equal(Pro, stored.RecordedByUserId);
    }

    [Fact]
    public async Task EditMeasurement_RecordedByTheClient_IsForbiddenForTheProfessional()
    {
        var (db, _, own, _) = await Seed();

        var result = await Measurements(db).EditAsync(own, new AddMeasurementCommand(Day, WeightKg: 70m), Pro, Client, default);

        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Equal(81m, db.Measurements.Single(m => m.Id == own).WeightKg);
    }

    [Fact]
    public async Task EditMeasurement_ClientCannotRewriteTheProfessionalsRecord()
    {
        var (db, id, _, _) = await Seed();

        var result = await Measurements(db).EditAsync(id, new AddMeasurementCommand(Day, WeightKg: 60m), Client, Client, default);

        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task EditMeasurement_AfterLinkEnded_IsForbidden()
    {
        var (db, id, _, _) = await Seed();

        var result = await Measurements(db, linked: false).EditAsync(id, new AddMeasurementCommand(Day, WeightKg: 79m), Pro, Client, default);

        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task EditMeasurement_InvalidValues_Returns400_UnknownId_Returns404()
    {
        var (db, id, _, _) = await Seed();
        var handler = Measurements(db);

        Assert.Equal(400, (await handler.EditAsync(id, new AddMeasurementCommand(Day), Pro, Client, default)).Error!.StatusCode);
        Assert.Equal(404, (await handler.EditAsync(999, new AddMeasurementCommand(Day, WeightKg: 70m), Pro, Client, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task EditMeasurement_OfAnotherUser_IsNotFound()
    {
        var (db, id, _, _) = await Seed();

        // Same id asked under a different target: must not leak or touch the other user's record.
        var result = await new ManageMeasurementHandler(
            db, NutritionProfessionalTestSupport.Policy((Pro, 3)).Object, new AddMeasurementCommandValidator())
            .EditAsync(id, new AddMeasurementCommand(Day, WeightKg: 70m), Pro, 3, default);

        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task DeleteMeasurement_ByRecorder_Removes_OtherwiseForbidden()
    {
        var (db, id, own, _) = await Seed();
        var handler = Measurements(db);

        Assert.Equal(403, (await handler.DeleteAsync(own, Pro, Client, default)).Error!.StatusCode);
        Assert.True((await handler.DeleteAsync(id, Pro, Client, default)).IsSuccess);
        Assert.DoesNotContain(db.Measurements, m => m.Id == id);
        Assert.Contains(db.Measurements, m => m.Id == own);
    }

    [Fact]
    public async Task EditComment_ByAuthor_ChangesOnlyText()
    {
        var (db, _, _, id) = await Seed();

        var result = await Comments(db).EditAsync(id, new EditDiaryCommentCommand("  novo texto "), Pro, Client, default);

        Assert.True(result.IsSuccess);
        var stored = db.DiaryComments.Single();
        Assert.Equal("novo texto", stored.Text);
        Assert.Equal(Day, stored.Date);
    }

    [Fact]
    public async Task EditComment_ByAnotherLinkedProfessional_OrAfterUnlink_IsForbidden()
    {
        var (db, _, _, id) = await Seed();
        var other = new ManageDiaryCommentHandler(
            db, NutritionProfessionalTestSupport.Policy((9, Client)).Object, Mock.Of<IUserRepository>(), new EditDiaryCommentCommandValidator());

        Assert.Equal(403, (await other.EditAsync(id, new EditDiaryCommentCommand("x"), 9, Client, default)).Error!.StatusCode);
        Assert.Equal(403, (await Comments(db, linked: false).EditAsync(id, new EditDiaryCommentCommand("x"), Pro, Client, default)).Error!.StatusCode);
        Assert.Equal("ok", db.DiaryComments.Single().Text);
    }

    [Fact]
    public async Task EditComment_ClientCannotEditOnTheirOwnDiary_AndEmptyTextIs400()
    {
        var (db, _, _, id) = await Seed();

        Assert.Equal(403, (await Comments(db).EditAsync(id, new EditDiaryCommentCommand("x"), Client, Client, default)).Error!.StatusCode);
        Assert.Equal(400, (await Comments(db).EditAsync(id, new EditDiaryCommentCommand(" "), Pro, Client, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_ByAuthor_Removes_UnknownIs404()
    {
        var (db, _, _, id) = await Seed();
        var handler = Comments(db);

        Assert.Equal(404, (await handler.DeleteAsync(999, Pro, Client, default)).Error!.StatusCode);
        Assert.True((await handler.DeleteAsync(id, Pro, Client, default)).IsSuccess);
        Assert.Empty(db.DiaryComments);
    }
}
