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
        var upSql = sql.Split("protected override void Down", 2)[0];

        foreach (var name in CompoundExerciseNames)
        {
            var groups = Regex
                .Matches(upSql, $@"\(N'{Regex.Escape(name)}',\s*CAST\((\d+) AS bigint\)")
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .ToArray();
            Assert.True(
                groups.Length >= 2,
                $"Expected at least two distinct MuscleGroup values for '{name}' in Up(), found {groups.Length}.");
        }

        Assert.DoesNotContain("CAST(7 AS bigint)", upSql);
        Assert.DoesNotContain("CAST(448 AS bigint)", upSql);
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
