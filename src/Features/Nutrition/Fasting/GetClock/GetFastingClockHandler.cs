using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.GetClock;

public sealed class GetFastingClockHandler(FastingStateReader stateReader)
{
    public async Task<Result<FastingSnapshotResponse>> HandleAsync(
        GetFastingClockQuery query,
        int actorUserId,
        CancellationToken cancellationToken) =>
        Result<FastingSnapshotResponse>.Success(await stateReader.SnapshotAsync(actorUserId, cancellationToken));
}
