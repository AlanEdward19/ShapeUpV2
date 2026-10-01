using FluentValidation;
using ShapeUp.Features.Nutrition.Hydration.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;

public class SetHydrationDayHandler(
    IHydrationRepository repository,
    IValidator<SetHydrationDayCommand> validator)
{
    public async Task<Result<HydrationDayResponse>> HandleAsync(SetHydrationDayCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<HydrationDayResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var existing = await repository.GetDayAsync(actorUserId, command.Date, cancellationToken);

        // Same total: nothing changes (retry). Older client write than the stored one: the newer write wins.
        if (existing is not null
            && (existing.TotalMl == command.TotalMl
                || (command.ClientUpdatedAtUtc is { } clientAt && clientAt < existing.UpdatedAtUtc)))
        {
            return Result<HydrationDayResponse>.Success(new HydrationDayResponse(command.Date, existing.TotalMl, existing.UpdatedAtUtc));
        }

        var nowUtc = DateTime.UtcNow;
        await repository.UpsertDayAsync(actorUserId, command.Date, command.TotalMl, nowUtc, cancellationToken);

        return Result<HydrationDayResponse>.Success(new HydrationDayResponse(command.Date, command.TotalMl, nowUtc));
    }
}
