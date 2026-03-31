namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

using FluentValidation;
using ShapeUp.Features.GymManagement.TrainerClients.Shared;
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
    IValidator<GenerateTrainerClientInviteCommand> validator)
{
    private const string InviteTemplateId = "46dbcd80-c134-407d-ad36-2fe01ed0ca89";
    private const string InviteSubject = "Convite para ingressar na ShapeUp";

    public async Task<Result<GenerateTrainerClientInviteResponse>> HandleAsync(
        GenerateTrainerClientInviteCommand command,
        int trainerId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<GenerateTrainerClientInviteResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        if (command.TrainerPlanId.HasValue)
        {
            var plan = await trainerPlanRepository.GetByIdAsync(command.TrainerPlanId.Value, cancellationToken);
            if (plan is null)
                return Result<GenerateTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanNotFound(command.TrainerPlanId.Value));

            if (plan.TrainerId != trainerId)
                return Result<GenerateTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(plan.Id, trainerId));
        }

        var normalizedEmail = command.GetClientEmail().Trim().ToLowerInvariant();
        var activeInvite = await inviteRepository.GetActiveByTrainerAndEmailAsync(trainerId, normalizedEmail, cancellationToken);
        if (activeInvite is not null)
        {
            activeInvite.Status = TrainerClientInviteStatus.Revoked;
            await inviteRepository.UpdateAsync(activeInvite, cancellationToken);
        }

        var accessToken = TrainerClientInviteTokenCodec.GenerateToken();
        var invite = new TrainerClientInvite
        {
            TrainerId = trainerId,
            InviteeEmail = normalizedEmail,
            AccessTokenHash = TrainerClientInviteTokenCodec.ComputeHash(accessToken),
            TrainerPlanId = command.TrainerPlanId,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(command.ExpiresInHours ?? 24),
            Status = TrainerClientInviteStatus.Invited
        };

        await inviteRepository.AddAsync(invite, cancellationToken);

        var registerUrl = registerUrlBuilder.BuildRegisterUrl(invite.TrainerId, accessToken);
        var sendResult = await emailNotificationSender.SendTemplateAsync(
            new SendTemplateEmailRequest(
                invite.InviteeEmail,
                InviteSubject,
                InviteTemplateId,
                new Dictionary<string, object?>
                {
                    ["register_url"] = registerUrl,
                    ["trainer_name"] = command.GetTrainerName().Trim()
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
}



