using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.SendEmailTemplate;
using ShapeUp.Features.Nutrition.Moderation.Shared.Options;
using ShapeUp.Features.Nutrition.Moderation.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Moderation.DecideModeration;

public class DecideModerationHandler(
    IFoodModerationRepository foodModerationRepository,
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IUserRepository userRepository,
    SendEmailTemplateHandler sendEmailTemplateHandler,
    IOptions<NutritionModerationEmailOptions> emailOptions,
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
        else if (string.Equals(command.Decision, "Rejected", StringComparison.Ordinal))
        {
            await NotifyAuthorOfRejectionAsync(request, cancellationToken);
        }

        return Result<DecideModerationResponse>.Success(
            new DecideModerationResponse(command.RequestId, command.Decision, decidedAtUtc));
    }

    private async Task NotifyAuthorOfRejectionAsync(
        FoodModerationRequestDocument request,
        CancellationToken cancellationToken)
    {
        var options = emailOptions.Value;
        if (string.IsNullOrWhiteSpace(options.RejectionTemplateId))
            return;

        var author = await userRepository.GetByIdAsync(request.RequestedByUserId, cancellationToken);
        if (author is null || string.IsNullOrWhiteSpace(author.Email))
            return;

        var food = await foodRepository.GetByIdAsync(request.FoodId, cancellationToken);
        var variables = new Dictionary<string, JsonElement>
        {
            ["foodName"] = JsonDocument.Parse(JsonSerializer.Serialize(food?.Name ?? "food")).RootElement.Clone()
        };

        await sendEmailTemplateHandler.HandleAsync(
            new SendEmailTemplateCommand(
                author.Email,
                options.RejectionSubject,
                options.RejectionTemplateId,
                variables),
            cancellationToken);
    }
}
