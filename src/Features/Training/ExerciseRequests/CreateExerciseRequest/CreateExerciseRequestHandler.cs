using FluentValidation;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests.CreateExerciseRequest;

/// <summary>Any signed-in user can ask for a missing exercise. The requester always comes from the token, never the body.</summary>
public class CreateExerciseRequestHandler(
    IExerciseRequestRepository requestRepository,
    IEquipmentRepository equipmentRepository,
    IUserPlatformRoleRepository roleRepository,
    IProfessionalCapabilityService capabilityService,
    ExerciseRequestResponseBuilder responseBuilder,
    IExerciseRequestSideEffects sideEffects,
    IValidator<CreateExerciseRequestCommand> validator)
{
    public async Task<Result<ExerciseRequestResponse>> HandleAsync(
        CreateExerciseRequestCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ExerciseRequestResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var equipmentIds = (command.EquipmentIds ?? []).Distinct().ToArray();
        foreach (var equipmentId in equipmentIds)
        {
            if (await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken) is null)
                return Result<ExerciseRequestResponse>.Failure(TrainingErrors.EquipmentNotFound(equipmentId));
        }

        if (await requestRepository.CountPendingByUserAsync(actorUserId, cancellationToken) >= ExerciseRequestCatalog.MaxPendingPerUser)
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestLimitReached(ExerciseRequestCatalog.MaxPendingPerUser));

        var request = new ExerciseRequest
        {
            Status = ExerciseRequest.StatusPending,
            Name = command.Name.Trim(),
            Kind = ExerciseRequestCatalog.NormalizeKind(command.Kind)!,
            ExerciseType = command.ExerciseType,
            MuscleGroups = string.Join(',', (command.MuscleGroups ?? [])
                .Select(g => ExerciseRequestCatalog.NormalizeMuscleGroup(g)!).Distinct()),
            EquipmentIds = string.Join(',', equipmentIds),
            EquipmentOther = ExerciseRequestCatalog.NormalizeOptional(command.EquipmentOther),
            Notes = ExerciseRequestCatalog.NormalizeOptional(command.Notes),
            ReferenceUrl = ExerciseRequestCatalog.NormalizeOptional(command.ReferenceUrl),
            RequestedByUserId = actorUserId,
            RequestedByRole = await ResolveRoleAsync(actorUserId, cancellationToken),
            CreatedAtUtc = DateTime.UtcNow
        };

        await requestRepository.AddAsync(request, cancellationToken);
        await sideEffects.AuditAsync("created", actorUserId, request.Id, null, request.Name, cancellationToken);

        var response = (await responseBuilder.BuildAsync([request], includeReviewer: false, cancellationToken))[0];
        return Result<ExerciseRequestResponse>.Success(response);
    }

    private async Task<string> ResolveRoleAsync(int userId, CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(userId, cancellationToken);
        if (capabilities.Nutrition)
            return "Nutritionist";

        var roles = (await roleRepository.GetByUserIdAsync(userId, cancellationToken)).Select(r => r.Role).ToHashSet();
        if (roles.Contains(PlatformRoleType.Trainer) || capabilities.Training) return "Trainer";
        if (roles.Contains(PlatformRoleType.GymClient)) return "GymClient";
        if (roles.Contains(PlatformRoleType.Client)) return "Client";
        return "IndependentClient";
    }
}
