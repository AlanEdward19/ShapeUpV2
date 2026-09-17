using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.Workouts;

public class WorkoutExerciseDtoValidatorTests
{
    private readonly WorkoutExerciseDtoValidator _sut = new();

    private static WorkoutSetValueObject ValidSet(int? repetitions = 10, decimal load = 30m, IntensityDto? intensity = null, int? restSeconds = 90) =>
        new(repetitions, load, LoadUnit.Kg, SetType.Working, Technique.Straight, intensity, restSeconds);

    private static WorkoutExerciseDto ExerciseWith(params WorkoutSetValueObject[] sets) =>
        new(1, sets);

    [Fact]
    public void Validate_WhenAllFieldsValid_IsValid()
    {
        var exercise = ExerciseWith(ValidSet());

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenRepetitionsIsNull_IsInvalid()
    {
        var exercise = ExerciseWith(ValidSet(repetitions: null));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenRepetitionsIsZeroOrNegative_IsInvalid(int repetitions)
    {
        var exercise = ExerciseWith(ValidSet(repetitions: repetitions));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenLoadIsNegative_IsInvalid()
    {
        var exercise = ExerciseWith(ValidSet(load: -1m));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenLoadIsZeroAndRepetitionsIsOne_IsValid()
    {
        var exercise = ExerciseWith(ValidSet(repetitions: 1, load: 0m));

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenIntensityIsNull_IsValid()
    {
        var exercise = ExerciseWith(ValidSet(intensity: null));

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Validate_WhenIntensityValueIsOutOfRange_IsInvalid(int intensityValue)
    {
        var exercise = ExerciseWith(ValidSet(intensity: new IntensityDto(IntensityType.Rpe, intensityValue)));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    public void Validate_WhenIntensityValueIsWithinInclusiveRange_IsValid(int intensityValue)
    {
        var exercise = ExerciseWith(ValidSet(intensity: new IntensityDto(IntensityType.Rpe, intensityValue)));

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenExerciseIdIsZeroOrNegative_IsInvalid(int exerciseId)
    {
        var exercise = new WorkoutExerciseDto(exerciseId, [ValidSet()]);

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenSetsIsEmpty_IsInvalid()
    {
        var exercise = new WorkoutExerciseDto(1, []);

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }
}
