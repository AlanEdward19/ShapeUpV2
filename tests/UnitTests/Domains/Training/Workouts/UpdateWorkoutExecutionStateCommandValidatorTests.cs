using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

namespace UnitTests.Domains.Training.Workouts;

public class UpdateWorkoutExecutionStateCommandValidatorTests
{
    private readonly UpdateWorkoutExecutionStateCommandValidator _sut = new();

    private static WorkoutSetValueObject ValidSet(int? repetitions = 10, decimal load = 30m, IntensityDto? intensity = null, int? restSeconds = 90) =>
        new(repetitions, load, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity, restSeconds);

    private static UpdateWorkoutExecutionStateCommand CommandWith(params WorkoutSetValueObject[] sets) =>
        new("session-1", DateTime.UtcNow, [new WorkoutExerciseDto(1, sets)]);

    [Fact]
    public void Validate_WhenSetIntensityIsNull_IsValid()
    {
        var command = CommandWith(ValidSet(intensity: null));

        var result = _sut.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRepetitionsIsNull_IsInvalid()
    {
        var command = CommandWith(ValidSet(repetitions: null));

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenLoadIsNegative_IsInvalid()
    {
        var command = CommandWith(ValidSet(load: -1m));

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIntensityValueOutOfRange_IsInvalid()
    {
        var command = CommandWith(ValidSet(intensity: new IntensityDto(IntensityType.Rpe, 11)));

        var result = _sut.Validate(command);

        Assert.False(result.IsValid);
    }
}
