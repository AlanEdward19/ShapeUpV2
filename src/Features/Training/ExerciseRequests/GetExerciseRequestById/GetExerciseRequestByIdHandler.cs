using ShapeUp.Features.Authorization.Resolver;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests.GetExerciseRequestById;

public record GetExerciseRequestByIdQuery(int RequestId);

/// <summary>An admin reads any request, the requester only their own (anyone else gets 404, like a missing id).</summary>
public class GetExerciseRequestByIdHandler(
    IExerciseRequestRepository repository,
    ICapabilityResolver capabilityResolver,
    ExerciseRequestResponseBuilder responseBuilder)
{
    public const string ManageCapability = "platform.exercises.manage";

    public async Task<Result<ExerciseRequestResponse>> HandleAsync(
        GetExerciseRequestByIdQuery query, int actorUserId, CancellationToken cancellationToken)
    {
        var request = await repository.GetByIdAsync(query.RequestId, cancellationToken);
        if (request is null)
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestNotFound(query.RequestId));

        var isOwner = request.RequestedByUserId == actorUserId;
        var isAdmin = !isOwner && (await capabilityResolver.ResolveAsync(
            actorUserId, ManageCapability, new AuthorizationContext(RequiresPlatformAdmin: true), cancellationToken)).IsAllowed;
        if (!isOwner && !isAdmin)
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestNotFound(query.RequestId));

        var response = (await responseBuilder.BuildAsync([request], includeReviewer: isAdmin, cancellationToken))[0];
        return Result<ExerciseRequestResponse>.Success(response);
    }
}
