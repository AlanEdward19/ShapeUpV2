namespace ShapeUp.Features.Authorization.Groups.UpdateUserRole;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class UpdateUserRoleHandler(IGroupRepository groupRepository)
{
    public async Task<Result<UpdateUserRoleResponse>> HandleAsync(
        int groupId,
        UpdateUserRoleCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var currentUserRole = await groupRepository.GetUserRoleInGroupAsync(currentUserId, groupId, cancellationToken);
        if (currentUserRole != GroupRole.Owner)
        {
            return Result<UpdateUserRoleResponse>.Failure(
                AuthorizationErrors.MissingPermission("Only group owners can change user roles."));
        }

        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group == null)
            return Result<UpdateUserRoleResponse>.Failure(AuthorizationErrors.GroupNotFound(groupId));

        var userBelongs = await groupRepository.UserBelongsToGroupAsync(command.UserId, groupId, cancellationToken);
        if (!userBelongs)
        {
            return Result<UpdateUserRoleResponse>.Failure(
                AuthorizationErrors.GroupMemberNotFound(command.UserId, groupId));
        }

        if (!Enum.TryParse<GroupRole>(command.NewRole, ignoreCase: true, out var role))
            return Result<UpdateUserRoleResponse>.Failure(AuthorizationErrors.InvalidRole(command.NewRole));

        await groupRepository.UpdateUserRoleAsync(command.UserId, groupId, role, cancellationToken);

        var response = new UpdateUserRoleResponse(command.UserId, groupId, role.ToString());
        return Result<UpdateUserRoleResponse>.Success(response);
    }
}
