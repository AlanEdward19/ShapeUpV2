using System.Text.RegularExpressions;

namespace UnitTests.Domains.Training.Exercises;

public class SeedExerciseCompoundMuscleProfilesTests
{
    private static readonly string[] CompoundExerciseNames =
    [
        "Barbell Bench Press",
        "Barbell Back Squat",
        "Barbell Row",
        "Pull-Up",
        "Lat Pulldown",
        "Barbell Overhead Press",
        "Barbell Romanian Deadlift",
        "Barbell Hip Thrust",
        "Close-Grip Bench Press"
    ];

    [Fact]
    public void MigrationSql_EachNamedCompoundAppearsAtLeastTwiceWithLeafMuscleGroups()
    {
        var sql = File.ReadAllText(MigrationFilePath());

        foreach (var name in CompoundExerciseNames)
        {
            var count = Regex.Matches(sql, $@"\(N'{Regex.Escape(name)}'").Count;
            Assert.True(count >= 2, $"Expected at least two muscle profile rows for '{name}', found {count}.");
        }

        Assert.DoesNotContain("CAST(7 AS bigint)", sql);
        Assert.DoesNotContain("CAST(448 AS bigint)", sql);
    }

    private static string MigrationFilePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "Features",
                "Training",
                "Infrastructure",
                "Data",
                "Migrations",
                "20260919120000_SeedExerciseCompoundMuscleProfiles.cs");
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("Could not locate 20260919120000_SeedExerciseCompoundMuscleProfiles.cs from test output directory.");
    }
}
