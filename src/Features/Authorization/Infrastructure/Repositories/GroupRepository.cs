namespace ShapeUp.Features.Authorization.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using Shared.Data;
using Shared.Entities;

public class GroupRepository(AuthorizationDbContext context) : IGroupRepository
{
    public async Task<Group?> GetByIdAsync(int groupId, CancellationToken cancellationToken) =>
        await context.Groups
            .Include(g => g.Members)
            .Include(g => g.Scopes)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByUserIdAsync(int userId, CancellationToken cancellationToken) =>
        await context.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .Include(g => g.Members)
            .Include(g => g.Scopes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByUserIdKeysetAsync(int userId, int? lastGroupId, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.Groups
            .Where(g => g.Members.Any(m => m.UserId == userId));

        if (lastGroupId.HasValue)
            query = query.Where(g => g.Id < lastGroupId.Value);

        return await query
            .OrderByDescending(g => g.Id)
            .Take(pageSize)
            .Include(g => g.Members)
            .Include(g => g.Scopes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Groups
            .Include(g => g.Members)
            .Include(g => g.Scopes)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken)
    {
        await context.Groups.AddAsync(group, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Group group, CancellationToken cancellationToken)
    {
        group.UpdatedAt = DateTime.UtcNow;
        context.Groups.Update(group);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int groupId, CancellationToken cancellationToken)
    {
        await context.Groups
            .Where(g => g.Id == groupId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> UserBelongsToGroupAsync(int userId, int groupId, CancellationToken cancellationToken) =>
        await context.UserGroups
            .AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId, cancellationToken);

    public async Task<GroupRole?> GetUserRoleInGroupAsync(int userId, int groupId, CancellationToken cancellationToken) =>
        await context.UserGroups
            .Where(ug => ug.UserId == userId && ug.GroupId == groupId)
            .Select(ug => (GroupRole?)ug.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddUserToGroupAsync(int userId, int groupId, GroupRole role, CancellationToken cancellationToken)
    {
        var userGroup = new UserGroup { UserId = userId, GroupId = groupId, Role = role };
        await context.UserGroups.AddAsync(userGroup, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUserFromGroupAsync(int userId, int groupId, CancellationToken cancellationToken)
    {
        await context.UserGroups
            .Where(ug => ug.UserId == userId && ug.GroupId == groupId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task UpdateUserRoleAsync(int userId, int groupId, GroupRole role, CancellationToken cancellationToken)
    {
        await context.UserGroups
            .Where(ug => ug.UserId == userId && ug.GroupId == groupId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(ug => ug.Role, role), cancellationToken);
    }

    public async Task<IReadOnlyList<UserGroupDto>> GetGroupMembersAsync(int groupId, CancellationToken cancellationToken)
    {
        var members = await context.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .Include(ug => ug.User)
            .Select(ug => new UserGroupDto(
                ug.User!.Id,
                ug.User.Email,
                ug.User.DisplayName,
                ug.Role
            ))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return members;
    }
}


