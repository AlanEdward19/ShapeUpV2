namespace ShapeUp.Features.GymManagement.TrainerClients.AcceptTrainerClientInvite;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Features.GymManagement.Shared.Security;
using ShapeUp.Shared.Results;

public class AcceptTrainerClientInviteHandler(
    ITrainerClientInviteRepository inviteRepository,
    ITrainerClientRepository trainerClientRepository,
    ITrainerPlanRepository trainerPlanRepository,
    IGymClientRepository gymClientRepository,
    IUserPlatformRoleRepository roleRepository,
    IValidator<AcceptTrainerClientInviteCommand> validator)
{
    public async Task<Result<AcceptTrainerClientInviteResponse>> HandleAsync(
        AcceptTrainerClientInviteCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<AcceptTrainerClientInviteResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        var tokenHash = TrainerClientInviteTokenCodec.ComputeHash(command.AccessToken.Trim());
        var invite = await inviteRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (invite is null)
            return Result<AcceptTrainerClientInviteResponse>.Failure(GymManagementErrors.TrainerClientInviteNotFound());

        if (invite.Status != TrainerClientInviteStatus.Invited)
            return Result<AcceptTrainerClientInviteResponse>.Failure(GymManagementErrors.TrainerClientInviteNotAvailable());

        var nowUtc = DateTime.UtcNow;
        if (invite.ExpiresAtUtc <= nowUtc)
        {
            invite.Status = TrainerClientInviteStatus.Expired;
            await inviteRepository.UpdateAsync(invite, cancellationToken);
            return Result<AcceptTrainerClientInviteResponse>.Failure(GymManagementErrors.TrainerClientInviteExpired());
        }

        var existingTrainerRelationship = await trainerClientRepository.GetByClientIdAsync(currentUserId, cancellationToken);
        if (existingTrainerRelationship is not null)
            return Result<AcceptTrainerClientInviteResponse>.Failure(
                GymManagementErrors.ClientAlreadyUnderTrainer(currentUserId, existingTrainerRelationship.TrainerId));

        var existingGymRelationship = await gymClientRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (existingGymRelationship is not null)
            return Result<AcceptTrainerClientInviteResponse>.Failure(
                GymManagementErrors.ClientCannotBeTrainerAndGymClientAtSameTime(currentUserId));

        if (invite.TrainerPlanId.HasValue)
        {
            var plan = await trainerPlanRepository.GetByIdAsync(invite.TrainerPlanId.Value, cancellationToken);
            if (plan is null)
                return Result<AcceptTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanNotFound(invite.TrainerPlanId.Value));

            if (plan.TrainerId != invite.TrainerId)
                return Result<AcceptTrainerClientInviteResponse>.Failure(
                    GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(plan.Id, invite.TrainerId));
        }

        var trainerClient = new TrainerClient
        {
            TrainerId = invite.TrainerId,
            ClientId = currentUserId,
            TrainerPlanId = invite.TrainerPlanId
        };

        await trainerClientRepository.AddAsync(trainerClient, cancellationToken);

        var trainerClientRole = await roleRepository.GetByUserIdAndRoleAsync(currentUserId, PlatformRoleType.Client, cancellationToken);
        if (trainerClientRole is null)
        {
            await roleRepository.AddAsync(new UserPlatformRole
            {
                UserId = currentUserId,
                Role = PlatformRoleType.Client
            }, cancellationToken);
        }

        var independentRole = await roleRepository.GetByUserIdAndRoleAsync(currentUserId, PlatformRoleType.IndependentClient, cancellationToken);
        if (independentRole is not null)
            await roleRepository.DeleteAsync(independentRole.Id, cancellationToken);

        var gymClientRole = await roleRepository.GetByUserIdAndRoleAsync(currentUserId, PlatformRoleType.GymClient, cancellationToken);
        if (gymClientRole is not null)
            await roleRepository.DeleteAsync(gymClientRole.Id, cancellationToken);

        invite.Status = TrainerClientInviteStatus.Accepted;
        invite.AcceptedByUserId = currentUserId;
        invite.AcceptedAtUtc = nowUtc;
        await inviteRepository.UpdateAsync(invite, cancellationToken);

        return Result<AcceptTrainerClientInviteResponse>.Success(
            new AcceptTrainerClientInviteResponse(
                trainerClient.Id,
                trainerClient.TrainerId,
                trainerClient.ClientId,
                trainerClient.TrainerPlanId,
                trainerClient.EnrolledAt));
    }
}


