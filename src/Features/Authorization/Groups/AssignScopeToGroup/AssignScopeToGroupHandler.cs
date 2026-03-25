namespace ShapeUp.Features.Authorization.Groups.AssignScopeToGroup;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class AssignScopeToGroupHandler(IGroupRepository groupRepository, IScopeRepository scopeRepository)
{
    public async Task<Result<AssignScopeToGroupResponse>> HandleAsync(
        int groupId,
        AssignScopeToGroupCommand command,
        int currentUserId,
        CancellationToken cancellationToken)
    {
        var currentUserRole = await groupRepository.GetUserRoleInGroupAsync(currentUserId, groupId, cancellationToken);
        if (currentUserRole != GroupRole.Owner && currentUserRole != GroupRole.Administrator)
        {
            return Result<AssignScopeToGroupResponse>.Failure(
                AuthorizationErrors.MissingPermission("You do not have permission to assign scopes to this group."));
        }

        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group == null)
            return Result<AssignScopeToGroupResponse>.Failure(AuthorizationErrors.GroupNotFound(groupId));

        var scope = await scopeRepository.GetByIdAsync(command.ScopeId, cancellationToken);
        if (scope == null)
            return Result<AssignScopeToGroupResponse>.Failure(AuthorizationErrors.ScopeNotFound(command.ScopeId));

        await scopeRepository.AssignScopeToGroupAsync(groupId, command.ScopeId, cancellationToken);

        var response = new AssignScopeToGroupResponse(groupId, command.ScopeId, scope.Name);
        return Result<AssignScopeToGroupResponse>.Success(response);
    }
}
