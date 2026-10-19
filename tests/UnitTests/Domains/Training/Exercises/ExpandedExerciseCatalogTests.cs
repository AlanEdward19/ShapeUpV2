using Microsoft.EntityFrameworkCore.Migrations.Operations;
using ShapeUp.Features.Training.Exercises.GetExercises;
using ShapeUp.Features.Training.Infrastructure.Data.Migrations;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;

namespace UnitTests.Domains.Training.Exercises;

public class ExpandedExerciseCatalogTests
{
    [Theory]
    [InlineData(MuscleGroup.Pectoralis, "Peito")]
    [InlineData(MuscleGroup.Shoulders, "Ombros")]
    [InlineData(MuscleGroup.Biceps, "Bíceps")]
    [InlineData(MuscleGroup.Hamstrings, "Posterior de coxa")]
    [InlineData(MuscleGroup.Lats, "Dorsais")]
    [InlineData(MuscleGroup.UpperChest | MuscleGroup.Triceps, "Peito superior, Tríceps")]
    public async Task Catalog_ReturnsFamiliarPortugueseLabels(MuscleGroup group, string expected)
    {
        var repository = new Mock<IExerciseRepository>();
        repository.Setup(r => r.GetKeysetPageAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Exercise { Id = 1, Name = "Exercise", NamePt = "Exercício",
                MuscleProfiles = [new ExerciseMuscleProfile { MuscleGroup = group, ActivationPercent = 80 }] }]);
        var result = await new GetExercisesHandler(repository.Object).HandleAsync(new(null, 10), CancellationToken.None);
        Assert.Equal(expected, result.Value!.Items.Single().Muscles.Single().MuscleNamePt);
    }

    [Fact]
    public void Seed_GuardsExistingNamesAndOnlyAttachesNewRows()
    {
        var sql = Assert.IsType<SqlOperation>(Assert.Single(new ExpandExerciseCatalog().UpOperations)).Sql;
        Assert.Contains("WHERE NOT EXISTS (SELECT 1 FROM [Exercises]", sql);
        Assert.Contains("OR e.[NamePt] = c.[NamePt]", sql);
        Assert.Contains("FROM @Added", sql);
        Assert.DoesNotContain("UPDATE [Exercises]", sql);
        Assert.Empty(new ExpandExerciseCatalog().DownOperations);
    }
}
