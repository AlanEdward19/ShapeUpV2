namespace ShapeUp.Features.Authorization.Groups.DeleteGroup;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class DeleteGroupHandler(IGroupRepository groupRepository)
{
    public async Task<Result<DeleteGroupResponse>> HandleAsync(
        DeleteGroupCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var currentUserRole = await groupRepository.GetUserRoleInGroupAsync(currentUserId, command.GroupId, cancellationToken);
        if (currentUserRole != GroupRole.Owner)
        {
            return Result<DeleteGroupResponse>.Failure(
                AuthorizationErrors.MissingPermission("Only group owners can delete the group."));
        }

        var group = await groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group == null)
            return Result<DeleteGroupResponse>.Failure(AuthorizationErrors.GroupNotFound(command.GroupId));

        await groupRepository.DeleteAsync(command.GroupId, cancellationToken);

        var response = new DeleteGroupResponse(command.GroupId, "Group deleted successfully.");
        return Result<DeleteGroupResponse>.Success(response);
    }
}
