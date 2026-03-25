namespace ShapeUp.Features.Authorization.Groups.RemoveUserFromGroup;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class RemoveUserFromGroupHandler(IGroupRepository groupRepository)
{
    public async Task<Result<RemoveUserFromGroupResponse>> HandleAsync(
        RemoveUserFromGroupCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var currentUserRole = await groupRepository.GetUserRoleInGroupAsync(currentUserId, command.GroupId, cancellationToken);
        if (currentUserRole != GroupRole.Owner && currentUserRole != GroupRole.Administrator)
        {
            return Result<RemoveUserFromGroupResponse>.Failure(
                AuthorizationErrors.MissingPermission("You do not have permission to remove users from this group."));
        }

        var group = await groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group == null)
            return Result<RemoveUserFromGroupResponse>.Failure(AuthorizationErrors.GroupNotFound(command.GroupId));

        var userBelongs = await groupRepository.UserBelongsToGroupAsync(command.UserId, command.GroupId, cancellationToken);
        if (!userBelongs)
        {
            return Result<RemoveUserFromGroupResponse>.Failure(
                AuthorizationErrors.GroupMemberNotFound(command.UserId, command.GroupId));
        }

        await groupRepository.RemoveUserFromGroupAsync(command.UserId, command.GroupId, cancellationToken);

        var response = new RemoveUserFromGroupResponse(command.UserId, command.GroupId, "User removed from group successfully.");
        return Result<RemoveUserFromGroupResponse>.Success(response);
    }
}
