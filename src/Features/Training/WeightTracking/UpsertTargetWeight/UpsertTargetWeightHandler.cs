using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WeightTracking.UpsertTargetWeight;

public class UpsertTargetWeightHandler(
    IWeightTrackingRepository repository,
    IValidator<UpsertTargetWeightCommand> validator)
{
    public async Task<Result<TargetWeightResponse>> HandleAsync(
        UpsertTargetWeightCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<TargetWeightResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var nowUtc = DateTime.UtcNow;
        await repository.UpsertTargetWeightAsync(actorUserId, command.TargetWeight, nowUtc, cancellationToken);

        return Result<TargetWeightResponse>.Success(new TargetWeightResponse(command.TargetWeight, nowUtc));
    }
}
