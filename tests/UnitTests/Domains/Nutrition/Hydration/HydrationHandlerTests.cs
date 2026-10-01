namespace UnitTests.Domains.Nutrition.Hydration;

using ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;
using ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

public class HydrationHandlerTests
{
    private static readonly DateOnly Day = DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private readonly Mock<IHydrationRepository> _repository = new();

    private SetHydrationDayHandler SetHandler() => new(_repository.Object, new SetHydrationDayCommandValidator());

    private void Stored(int totalMl, DateTime updatedAtUtc) =>
        _repository
            .Setup(x => x.GetDayAsync(10, Day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HydrationDayDocument { UserId = 10, Day = Day.ToString("yyyy-MM-dd"), TotalMl = totalMl, UpdatedAtUtc = updatedAtUtc });

    private void VerifyNoWrite() =>
        _repository.Verify(x => x.UpsertDayAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    public async Task Set_WithTotalOutOfRange_ReturnsValidationFailure(int totalMl)
    {
        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, totalMl), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        VerifyNoWrite();
    }

    [Fact]
    public async Task Set_WithFarFutureDate_ReturnsValidationFailure()
    {
        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day.AddDays(30), 500), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        VerifyNoWrite();
    }

    [Fact]
    public async Task Set_AcceptsTomorrowAndRejectsTheDayAfter()
    {
        var tomorrow = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day.AddDays(1), 500), 10, CancellationToken.None);
        var dayAfter = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day.AddDays(2), 500), 10, CancellationToken.None);

        Assert.True(tomorrow.IsSuccess);
        Assert.True(dayAfter.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task Set_WithBoundaryTotal_Saves(int totalMl)
    {
        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, totalMl), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(totalMl, result.Value!.TotalMl);
        _repository.Verify(x => x.UpsertDayAsync(10, Day, totalMl, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Set_WithNewTotal_SavesAndReturnsUpdatedAt()
    {
        Stored(500, DateTime.UtcNow.AddHours(-1));

        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, 1750), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1750, result.Value!.TotalMl);
        Assert.True(result.Value.UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-1));
        _repository.Verify(x => x.UpsertDayAsync(10, Day, 1750, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Set_RepeatedWithSameTotal_ChangesNothing()
    {
        var storedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        Stored(1750, storedAt);

        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, 1750), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(storedAt, result.Value!.UpdatedAtUtc);
        VerifyNoWrite();
    }

    [Fact]
    public async Task Set_WithOlderClientTimestamp_KeepsNewerStoredValue()
    {
        var storedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        Stored(2000, storedAt);

        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, 1000, storedAt.AddMinutes(-5)), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000, result.Value!.TotalMl);
        VerifyNoWrite();
    }

    [Fact]
    public async Task Set_WithNewerClientTimestamp_Overwrites()
    {
        var storedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        Stored(2000, storedAt);

        var result = await SetHandler().HandleAsync(new SetHydrationDayCommand(Day, 1000, storedAt.AddMinutes(5)), 10, CancellationToken.None);

        Assert.Equal(1000, result.Value!.TotalMl);
        _repository.Verify(x => x.UpsertDayAsync(10, Day, 1000, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDay_WithoutRecord_ReturnsZero()
    {
        var result = await new GetHydrationDayHandler(_repository.Object).HandleAsync(Day, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalMl);
        Assert.Null(result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetDay_WithRecord_ReturnsIt()
    {
        var storedAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        Stored(1250, storedAt);

        var result = await new GetHydrationDayHandler(_repository.Object).HandleAsync(Day, 10, CancellationToken.None);

        Assert.Equal(1250, result.Value!.TotalMl);
        Assert.Equal(storedAt, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetRange_ReturnsRecordedDays()
    {
        _repository
            .Setup(x => x.GetRangeAsync(10, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new HydrationDayDocument { UserId = 10, Day = "2026-09-01", TotalMl = 2000, UpdatedAtUtc = DateTime.UtcNow },
                new HydrationDayDocument { UserId = 10, Day = "2026-09-03", TotalMl = 1500, UpdatedAtUtc = DateTime.UtcNow }
            ]);
        var sut = new GetHydrationRangeHandler(_repository.Object, new GetHydrationRangeQueryValidator());

        var result = await sut.HandleAsync(new GetHydrationRangeQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 3)], result.Value!.Days.Select(d => d.Date));
        Assert.Equal([2000, 1500], result.Value.Days.Select(d => d.TotalMl));
    }

    [Fact]
    public async Task GetRange_WithInvertedOrTooLongRange_ReturnsValidationFailure()
    {
        var sut = new GetHydrationRangeHandler(_repository.Object, new GetHydrationRangeQueryValidator());

        var inverted = await sut.HandleAsync(new GetHydrationRangeQuery(new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 1)), 10, CancellationToken.None);
        var tooLong = await sut.HandleAsync(new GetHydrationRangeQuery(new DateOnly(2025, 1, 1), new DateOnly(2026, 9, 1)), 10, CancellationToken.None);

        Assert.True(inverted.IsFailure);
        Assert.True(tooLong.IsFailure);
    }
}
