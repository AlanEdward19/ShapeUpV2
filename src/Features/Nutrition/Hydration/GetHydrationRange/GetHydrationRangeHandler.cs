using FluentValidation;
using ShapeUp.Features.Nutrition.Hydration.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;

public class GetHydrationRangeHandler(
    IHydrationRepository repository,
    IValidator<GetHydrationRangeQuery> validator)
{
    public async Task<Result<HydrationRangeResponse>> HandleAsync(GetHydrationRangeQuery query, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<HydrationRangeResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var days = await repository.GetRangeAsync(actorUserId, query.From, query.To, cancellationToken);

        return Result<HydrationRangeResponse>.Success(new HydrationRangeResponse(
            query.From,
            query.To,
            days.Select(x => new HydrationDayResponse(DateOnly.ParseExact(x.Day, "yyyy-MM-dd"), x.TotalMl, x.UpdatedAtUtc)).ToArray()));
    }
}
