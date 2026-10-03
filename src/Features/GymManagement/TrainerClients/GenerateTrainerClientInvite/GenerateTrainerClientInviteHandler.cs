namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

using FluentValidation;
using Microsoft.Extensions.Options;
using Shared;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Features.GymManagement.Shared.Security;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

public class GenerateTrainerClientInviteHandler(
    ITrainerClientInviteRepository inviteRepository,
    ITrainerPlanRepository trainerPlanRepository,
    IEmailNotificationSender emailNotificationSender,
    ITrainerClientInviteRegisterUrlBuilder registerUrlBuilder,
    IOptions<TrainerClientInviteEmailOptions> emailOptions,
    IValidator<GenerateTrainerClientInviteCommand> validator)
{
    private const string DefaultPlanName = "Acompanhamento personalizado";

    private readonly TrainerClientInviteEmailOptions _emailOptions = emailOptions.Value;

    public async Task<Result<GenerateTrainerClientInviteResponse>> HandleAsync(
        GenerateTrainerClientInviteCommand command,
        int trainerId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<GenerateTrainerClientInviteResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        var planName = DefaultPlanName;
        if (command.TrainerPlanId.HasValue)
        {
            var plan = await trainerPlanRepository.GetByIdAsync(command.TrainerPlanId.Value, cancellationToken);
            if (plan is null)
                return Result<GenerateTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanNotFound(command.TrainerPlanId.Value));

            if (plan.TrainerId != trainerId)
                return Result<GenerateTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(plan.Id, trainerId));

            planName = plan.Name;
        }

        var normalizedEmail = command.GetClientEmail().Trim().ToLowerInvariant();
        var activeInvite = await inviteRepository.GetActiveByTrainerAndEmailAsync(trainerId, normalizedEmail, cancellationToken);
        if (activeInvite is not null)
        {
            activeInvite.Status = TrainerClientInviteStatus.Revoked;
            await inviteRepository.UpdateAsync(activeInvite, cancellationToken);
        }

        var expiresInHours = command.ExpiresInHours ?? 24;
        var accessToken = TrainerClientInviteTokenCodec.GenerateToken();
        var invite = new TrainerClientInvite
        {
            TrainerId = trainerId,
            InviteeEmail = normalizedEmail,
            AccessTokenHash = TrainerClientInviteTokenCodec.ComputeHash(accessToken),
            TrainerPlanId = command.TrainerPlanId,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(expiresInHours),
            Status = TrainerClientInviteStatus.Invited
        };

        await inviteRepository.AddAsync(invite, cancellationToken);

        var registerUrl = registerUrlBuilder.BuildRegisterUrl(invite.TrainerId, accessToken);
        var sendResult = await emailNotificationSender.SendTemplateAsync(
            new SendTemplateEmailRequest(
                invite.InviteeEmail,
                _emailOptions.Subject,
                _emailOptions.TemplateId,
                new Dictionary<string, object?>
                {
                    ["register_url"] = registerUrl,
                    ["trainer_name"] = command.GetTrainerName().Trim(),
                    ["plan_name"] = planName,
                    ["expires_in"] = FormatExpiration(expiresInHours)
                }),
            cancellationToken);

        if (sendResult.IsFailure)
            return Result<GenerateTrainerClientInviteResponse>.Failure(sendResult.Error!);

        return Result<GenerateTrainerClientInviteResponse>.Success(
            new GenerateTrainerClientInviteResponse(
                invite.Id,
                invite.TrainerId,
                invite.InviteeEmail,
                accessToken,
                invite.ExpiresAtUtc,
                invite.Status.ToString()));
    }

    private static string FormatExpiration(int hours)
    {
        if (hours >= 24 && hours % 24 == 0)
        {
            var days = hours / 24;
            return days == 1 ? "1 dia" : $"{days} dias";
        }

        return hours == 1 ? "1 hora" : $"{hours} horas";
    }
}
