namespace ShapeUp.Features.Authorization.Groups.AddUserToGroup;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class AddUserToGroupHandler(IGroupRepository groupRepository, IUserRepository userRepository)
{
    public async Task<Result<AddUserToGroupResponse>> HandleAsync(
        AddUserToGroupCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var currentUserRole = await groupRepository.GetUserRoleInGroupAsync(currentUserId, command.GroupId, cancellationToken);
        if (currentUserRole != GroupRole.Owner && currentUserRole != GroupRole.Administrator)
        {
            return Result<AddUserToGroupResponse>.Failure(
                AuthorizationErrors.MissingPermission("You do not have permission to add users to this group."));
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return Result<AddUserToGroupResponse>.Failure(AuthorizationErrors.UserNotFound(command.UserId));

        var group = await groupRepository.GetByIdAsync(command.GroupId, cancellationToken);
        if (group == null)
            return Result<AddUserToGroupResponse>.Failure(AuthorizationErrors.GroupNotFound(command.GroupId));

        var alreadyExists = await groupRepository.UserBelongsToGroupAsync(command.UserId, command.GroupId, cancellationToken);
        if (alreadyExists)
        {
            return Result<AddUserToGroupResponse>.Failure(
                AuthorizationErrors.GroupMemberAlreadyExists(command.UserId, command.GroupId));
        }

        if (!Enum.TryParse<GroupRole>(command.Role, ignoreCase: true, out var role))
            return Result<AddUserToGroupResponse>.Failure(AuthorizationErrors.InvalidRole(command.Role));

        await groupRepository.AddUserToGroupAsync(command.UserId, command.GroupId, role, cancellationToken);

        var response = new AddUserToGroupResponse(command.UserId, command.GroupId, role.ToString());
        return Result<AddUserToGroupResponse>.Success(response);
    }
}
