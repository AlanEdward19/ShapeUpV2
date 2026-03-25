namespace ShapeUp.Features.Authorization.Shared.Abstractions;

using Entities;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Group>> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Group>> GetByUserIdKeysetAsync(int userId, int? lastGroupId, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
    Task UpdateAsync(Group group, CancellationToken cancellationToken);
    Task DeleteAsync(int groupId, CancellationToken cancellationToken);
    Task<bool> UserBelongsToGroupAsync(int userId, int groupId, CancellationToken cancellationToken);
    Task<GroupRole?> GetUserRoleInGroupAsync(int userId, int groupId, CancellationToken cancellationToken);
    Task AddUserToGroupAsync(int userId, int groupId, GroupRole role, CancellationToken cancellationToken);
    Task RemoveUserFromGroupAsync(int userId, int groupId, CancellationToken cancellationToken);
    Task UpdateUserRoleAsync(int userId, int groupId, GroupRole role, CancellationToken cancellationToken);
    Task<IReadOnlyList<UserGroupDto>> GetGroupMembersAsync(int groupId, CancellationToken cancellationToken);
}

public record UserGroupDto(int UserId, string Email, string? DisplayName, GroupRole Role);


