using FluentValidation;
using ShapeUp.Features.Nutrition.Moderation.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Moderation.DecideModeration;

public class DecideModerationHandler(
    IFoodModerationRepository foodModerationRepository,
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<DecideModerationCommand> validator)
{
    public async Task<Result<DecideModerationResponse>> HandleAsync(
        DecideModerationCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DecideModerationResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var request = await foodModerationRepository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
            return Result<DecideModerationResponse>.Failure(NutritionErrors.ModerationRequestNotFound(command.RequestId));

        if (!string.Equals(request.Status, "Pending", StringComparison.Ordinal))
            return Result<DecideModerationResponse>.Failure(NutritionErrors.ModerationRequestAlreadyDecided(command.RequestId));

        var overrideDocument = await foodOverrideRepository.GetByIdAsync(request.FoodOverrideId, cancellationToken);
        if (overrideDocument is null)
            return Result<DecideModerationResponse>.Failure(NutritionErrors.FoodOverrideNotFound(request.FoodId));

        var decidedAtUtc = DateTime.UtcNow;
        var decided = await foodModerationRepository.DecideAsync(
            command.RequestId,
            command.Decision,
            actorUserId,
            decidedAtUtc,
            cancellationToken);

        if (!decided)
            return Result<DecideModerationResponse>.Failure(NutritionErrors.ModerationRequestAlreadyDecided(command.RequestId));

        if (string.Equals(command.Decision, "Approved", StringComparison.Ordinal))
        {
            await foodRepository.ApplyApprovedOverrideAsync(overrideDocument, cancellationToken);
            await foodOverrideRepository.DeleteAsync(overrideDocument.Id, cancellationToken);
        }

        return Result<DecideModerationResponse>.Success(
            new DecideModerationResponse(command.RequestId, command.Decision, decidedAtUtc));
    }
}
