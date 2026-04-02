namespace UnitTests.Domains.Training.WeightTracking;

using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.WeightTracking.UpsertDailyWeightRegister;

public class UpsertDailyWeightRegisterHandlerTests
{
    private readonly Mock<IWeightTrackingRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenCommandIsInvalid_ReturnsValidationFailure()
    {
        var sut = new UpsertDailyWeightRegisterHandler(_repository.Object, new UpsertDailyWeightRegisterCommandValidator());

        var result = await sut.HandleAsync(new UpsertDailyWeightRegisterCommand(0), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
        _repository.Verify(
            x => x.UpsertDailyWeightAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<DateOnly>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDateIsProvided_UpsertsDayAndReturnsTargetWeight()
    {
        _repository
            .Setup(x => x.GetTargetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeightTargetDocument { UserId = 10, TargetWeight = 82.5m, UpdatedAtUtc = DateTime.UtcNow });

        var sut = new UpsertDailyWeightRegisterHandler(_repository.Object, new UpsertDailyWeightRegisterCommandValidator());
        var providedDate = new DateTime(2026, 4, 1, 19, 30, 0, DateTimeKind.Utc);

        var result = await sut.HandleAsync(new UpsertDailyWeightRegisterCommand(84.2m, providedDate), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(new DateOnly(2026, 4, 1), result.Value!.Date);
        Assert.Equal(84.2m, result.Value.Weight);
        Assert.Equal(82.5m, result.Value.TargetWeight);

        _repository.Verify(
            x => x.UpsertDailyWeightAsync(
                10,
                84.2m,
                new DateOnly(2026, 4, 1),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
