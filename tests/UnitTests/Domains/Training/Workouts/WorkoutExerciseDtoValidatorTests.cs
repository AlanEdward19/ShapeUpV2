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

    // --- time-based-exercises: TBE-03 backend defense-in-depth format checks ---

    [Fact]
    public void Validate_WhenTimeBasedShapedSetHasNoLoadOrRepetitionsButHasDuration_IsValid()
    {
        var timeBasedSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: 120);
        var exercise = ExerciseWith(timeBasedSet);

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDistanceMetersIsNegative_IsInvalid()
    {
        var exercise = ExerciseWith(new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: 120, DistanceMeters: -1m));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenDistanceMetersIsZero_IsValid()
    {
        var exercise = ExerciseWith(new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: 120, DistanceMeters: 0m));

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenDurationSecondsIsZeroOrNegative_IsInvalid(int durationSeconds)
    {
        var exercise = ExerciseWith(new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: durationSeconds));

        var result = _sut.Validate(exercise);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WhenWeightBasedShapedSetHasNoDurationOrDistance_IsValid()
    {
        // Regression: existing WeightBased-shaped payloads (both Load and Repetitions present)
        // must remain valid, unaffected by the DurationSeconds/DistanceMeters rules.
        var exercise = ExerciseWith(ValidSet());

        var result = _sut.Validate(exercise);

        Assert.True(result.IsValid);
    }
}
