using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WeightTracking.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WeightTracking.GetWeightRegisters;

public class GetWeightRegistersHandler(
    IWeightTrackingRepository repository,
    IValidator<GetWeightRegistersQuery> validator)
{
    public async Task<Result<GetWeightRegistersResponse>> HandleAsync(
        GetWeightRegistersQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<GetWeightRegistersResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var startDate = DateOnly.FromDateTime(query.StartDateUtc.Date);
        var endDate = DateOnly.FromDateTime(query.EndDateUtc.Date);

        var registers = await repository.GetRegistersByRangeAsync(actorUserId, startDate, endDate, cancellationToken);
        var target = await repository.GetTargetByUserIdAsync(actorUserId, cancellationToken);

        var items = registers
            .Select(x => new WeightRegisterItemResponse(DateOnly.ParseExact(x.Day, "yyyy-MM-dd"), x.Weight, x.UpdatedAtUtc))
            .ToArray();

        return Result<GetWeightRegistersResponse>.Success(
            new GetWeightRegistersResponse(startDate, endDate, target?.TargetWeight, items));
    }
}
