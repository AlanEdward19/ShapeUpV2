using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Workouts.Shared;

namespace UnitTests.Domains.Training.Workouts;

public class WorkoutSessionResponseMapperTests
{
    [Fact]
    public void Map_WhenSessionHasTimeBasedSet_IncludesDurationAndDistanceInResponse()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-1",
            TargetUserId = 30,
            ExecutedByUserId = 30,
            StartedAtUtc = DateTime.UtcNow,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Running",
                    Sets =
                    [
                        new ExecutedSetDocumentValueObject
                        {
                            Repetitions = null,
                            Load = null,
                            DurationSeconds = 1800,
                            DistanceMeters = 5000m
                        }
                    ]
                }
            ]
        };

        var sut = new WorkoutSessionResponseMapper();

        var response = sut.Map(session);

        var mappedSet = response.Exercises[0].Sets[0];
        Assert.Equal(1800, mappedSet.DurationSeconds);
        Assert.Equal(5000m, mappedSet.DistanceMeters);
        Assert.Null(mappedSet.Load);
        Assert.Null(mappedSet.Repetitions);
    }

    [Fact]
    public void Map_WhenSessionHasWeightBasedSet_LeavesDurationAndDistanceNull()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-2",
            TargetUserId = 30,
            ExecutedByUserId = 30,
            StartedAtUtc = DateTime.UtcNow,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 2,
                    ExerciseName = "Bench Press",
                    Sets =
                    [
                        new ExecutedSetDocumentValueObject
                        {
                            Repetitions = 8,
                            Load = 80m
                        }
                    ]
                }
            ]
        };

        var sut = new WorkoutSessionResponseMapper();

        var response = sut.Map(session);

        var mappedSet = response.Exercises[0].Sets[0];
        Assert.Equal(8, mappedSet.Repetitions);
        Assert.Equal(80m, mappedSet.Load);
        Assert.Null(mappedSet.DurationSeconds);
        Assert.Null(mappedSet.DistanceMeters);
    }
}
