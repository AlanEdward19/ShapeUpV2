using ShapeUp.Features.Nutrition.Hydration.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;

public class GetHydrationDayHandler(IHydrationRepository repository)
{
    public async Task<Result<HydrationDayResponse>> HandleAsync(DateOnly date, int actorUserId, CancellationToken cancellationToken)
    {
        var day = await repository.GetDayAsync(actorUserId, date, cancellationToken);

        return Result<HydrationDayResponse>.Success(day is null
            ? new HydrationDayResponse(date, 0, null)
            : new HydrationDayResponse(date, day.TotalMl, day.UpdatedAtUtc));
    }
}
