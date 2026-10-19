using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Data.Migrations;
using ShapeUp.Features.Training.Shared.Entities;

namespace IntegrationTests.Domains.Training;

public class ExpandedExerciseCatalogTests
{
    [Fact]
    public async Task Seed_IsCompleteIdempotentAndPreservesExistingEdits()
    {
        await using var sql = new MsSqlBuilder().Build();
        await sql.StartAsync();
        var options = new DbContextOptionsBuilder<TrainingDbContext>().UseSqlServer(sql.GetConnectionString()).Options;
        await using var db = new TrainingDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var existing = new Exercise { Name = "Concentration Curl", NamePt = "Minha rosca", Description = "Não alterar" };
        db.Exercises.Add(existing);
        await db.SaveChangesAsync();
        var migration = new ExpandExerciseCatalog();
        foreach (var operation in migration.UpOperations.Cast<SqlOperation>())
            await db.Database.ExecuteSqlRawAsync(operation.Sql);
        var exercises = await db.Exercises.AsNoTracking().Include(e => e.Steps)
            .Include(e => e.MuscleProfiles).Include(e => e.ExerciseEquipments).ToListAsync();
        Assert.Equal(28, exercises.Count);
        foreach (var exercise in exercises.Where(e => e.Id != existing.Id))
        {
            Assert.Equal(2, exercise.Steps.Count);
            Assert.NotEmpty(exercise.MuscleProfiles);
            Assert.Single(exercise.ExerciseEquipments);
            Assert.False(string.IsNullOrWhiteSpace(exercise.NamePt));
            Assert.All(exercise.MuscleProfiles, p => Assert.True(((long)p.MuscleGroup & ((long)p.MuscleGroup - 1)) == 0));
        }
        foreach (var operation in migration.UpOperations.Cast<SqlOperation>())
            await db.Database.ExecuteSqlRawAsync(operation.Sql);
        Assert.Equal(28, await db.Exercises.CountAsync());
        Assert.Equal(54, await db.ExerciseSteps.CountAsync());
        Assert.Equal(27, await db.ExerciseEquipments.CountAsync());
        Assert.Equal("Não alterar", (await db.Exercises.AsNoTracking().SingleAsync(e => e.Id == existing.Id)).Description);
        Assert.Empty(migration.DownOperations);
    }
}
