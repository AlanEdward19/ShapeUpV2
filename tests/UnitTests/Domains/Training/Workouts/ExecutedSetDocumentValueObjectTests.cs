using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

namespace UnitTests.Domains.Training.Workouts;

public class ExecutedSetDocumentValueObjectTests
{
    [Fact]
    public void Volume_WhenLoadAndRepetitionsPresent_ReturnsProduct()
    {
        var set = new ExecutedSetDocumentValueObject { Load = 100m, Repetitions = 8 };

        Assert.Equal(800m, set.Volume);
    }

    [Fact]
    public void Volume_WhenLoadIsNull_ReturnsZero()
    {
        var set = new ExecutedSetDocumentValueObject { Load = null, Repetitions = 8 };

        Assert.Equal(0m, set.Volume);
    }

    [Fact]
    public void Volume_WhenRepetitionsIsNull_ReturnsZero()
    {
        var set = new ExecutedSetDocumentValueObject { Load = 100m, Repetitions = null };

        Assert.Equal(0m, set.Volume);
    }

    [Fact]
    public void Volume_WhenLoadAndRepetitionsAreNull_ReturnsZero()
    {
        var set = new ExecutedSetDocumentValueObject { Load = null, Repetitions = null };

        Assert.Equal(0m, set.Volume);
    }
}
