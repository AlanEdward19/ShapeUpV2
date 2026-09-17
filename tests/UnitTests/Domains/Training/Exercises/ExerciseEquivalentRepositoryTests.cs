using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Shared.Entities;

namespace UnitTests.Domains.Training.Exercises;

public class ExerciseEquivalentRepositoryTests
{
    [Fact]
    public async Task SetThenGet_FromEitherSide_ReturnsPeer_EXVAR02()
    {
        await using var db = NewDb();
        await SeedExercises(db, 1, 2);
        var sut = new ExerciseEquivalentRepository(db);

        await sut.SetEquivalentAsync(1, 2, CancellationToken.None);

        var fromOne = await sut.GetEquivalentsAsync(1, CancellationToken.None);
        var fromTwo = await sut.GetEquivalentsAsync(2, CancellationToken.None);

        Assert.Single(fromOne);
        Assert.Equal(2, fromOne[0].Id);
        Assert.Single(fromTwo);
        Assert.Equal(1, fromTwo[0].Id);
    }

    [Fact]
    public async Task Set_WhenOrderReversed_StillCanonicalOneRowAndSymmetric()
    {
        await using var db = NewDb();
        await SeedExercises(db, 5, 3);
        var sut = new ExerciseEquivalentRepository(db);

        await sut.SetEquivalentAsync(5, 3, CancellationToken.None);

        var row = Assert.Single(db.ExerciseEquivalents);
        Assert.Equal(3, row.ExerciseId);
        Assert.Equal(5, row.EquivalentExerciseId);

        var fromThree = await sut.GetEquivalentsAsync(3, CancellationToken.None);
        var fromFive = await sut.GetEquivalentsAsync(5, CancellationToken.None);
        Assert.Equal(5, Assert.Single(fromThree).Id);
        Assert.Equal(3, Assert.Single(fromFive).Id);
    }

    [Fact]
    public async Task Set_WhenDuplicate_IsIdempotentSingleRow()
    {
        await using var db = NewDb();
        await SeedExercises(db, 1, 2);
        var sut = new ExerciseEquivalentRepository(db);

        await sut.SetEquivalentAsync(1, 2, CancellationToken.None);
        await sut.SetEquivalentAsync(2, 1, CancellationToken.None);

        Assert.Single(db.ExerciseEquivalents);
    }

    [Fact]
    public async Task Remove_ThenGetEitherSide_ReturnsEmpty()
    {
        await using var db = NewDb();
        await SeedExercises(db, 1, 2);
        var sut = new ExerciseEquivalentRepository(db);

        await sut.SetEquivalentAsync(1, 2, CancellationToken.None);
        await sut.RemoveEquivalentAsync(2, 1, CancellationToken.None);

        Assert.Empty(await sut.GetEquivalentsAsync(1, CancellationToken.None));
        Assert.Empty(await sut.GetEquivalentsAsync(2, CancellationToken.None));
        Assert.Empty(db.ExerciseEquivalents);
    }

    private static TrainingDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TrainingDbContext(options);
    }

    private static async Task SeedExercises(TrainingDbContext db, params int[] ids)
    {
        foreach (var id in ids)
        {
            db.Exercises.Add(new Exercise
            {
                Id = id,
                Name = $"Ex{id}",
                NamePt = $"Ex{id}"
            });
        }

        await db.SaveChangesAsync();
    }
}
