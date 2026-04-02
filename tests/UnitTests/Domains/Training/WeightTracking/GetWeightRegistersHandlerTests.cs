namespace UnitTests.Domains.Training.WeightTracking;

using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.WeightTracking.GetWeightRegisters;

public class GetWeightRegistersHandlerTests
{
    private readonly Mock<IWeightTrackingRepository> _repository = new();

    [Fact]
    public async Task HandleAsync_WhenRangeIsInvalid_ReturnsValidationFailure()
    {
        var sut = new GetWeightRegistersHandler(_repository.Object, new GetWeightRegistersQueryValidator());

        var result = await sut.HandleAsync(
            new GetWeightRegistersQuery(new DateTime(2026, 4, 5), new DateTime(2026, 4, 1)),
            10,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenRangeIsValid_ReturnsRegistersAndTargetWeight()
    {
        _repository
            .Setup(x => x.GetRegistersByRangeAsync(
                10,
                new DateOnly(2026, 4, 1),
                new DateOnly(2026, 4, 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new WeightRegisterDocument
                {
                    UserId = 10,
                    Day = "2026-04-01",
                    Weight = 84.2m,
                    UpdatedAtUtc = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                new WeightRegisterDocument
                {
                    UserId = 10,
                    Day = "2026-04-02",
                    Weight = 83.9m,
                    UpdatedAtUtc = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc)
                }
            ]);

        _repository
            .Setup(x => x.GetTargetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeightTargetDocument { UserId = 10, TargetWeight = 82.5m });

        var sut = new GetWeightRegistersHandler(_repository.Object, new GetWeightRegistersQueryValidator());

        var result = await sut.HandleAsync(
            new GetWeightRegistersQuery(new DateTime(2026, 4, 1), new DateTime(2026, 4, 3)),
            10,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(82.5m, result.Value!.TargetWeight);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(84.2m, result.Value.Items[0].Weight);
        Assert.Equal(new DateOnly(2026, 4, 1), result.Value.Items[0].Date);
    }
}
