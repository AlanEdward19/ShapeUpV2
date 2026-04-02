using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WeightTracking.UpsertDailyWeightRegister;

public class UpsertDailyWeightRegisterHandler(
    IWeightTrackingRepository repository,
    IValidator<UpsertDailyWeightRegisterCommand> validator)
{
    public async Task<Result<UpsertDailyWeightRegisterResponse>> HandleAsync(
        UpsertDailyWeightRegisterCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UpsertDailyWeightRegisterResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var nowUtc = DateTime.UtcNow;
        var day = DateOnly.FromDateTime((command.DateUtc ?? nowUtc).Date);

        await repository.UpsertDailyWeightAsync(actorUserId, command.Weight, day, nowUtc, cancellationToken);
        var target = await repository.GetTargetByUserIdAsync(actorUserId, cancellationToken);

        return Result<UpsertDailyWeightRegisterResponse>.Success(
            new UpsertDailyWeightRegisterResponse(day, command.Weight, target?.TargetWeight, nowUtc));
    }
}
