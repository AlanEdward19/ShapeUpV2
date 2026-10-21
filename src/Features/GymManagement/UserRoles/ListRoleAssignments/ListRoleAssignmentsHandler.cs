namespace ShapeUp.Features.GymManagement.UserRoles.ListRoleAssignments;

using Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public record ListRoleAssignmentsQuery(string? Cursor, int? PageSize);

public record RoleAssignmentResponse(
    int Id,
    int UserId,
    string Role,
    int? PlatformTierId,
    string? TierName,
    bool IsActive,
    int? GrantedByCredentialId,
    DateTime CreatedAt);

public class ListRoleAssignmentsHandler(IUserPlatformRoleRepository repository)
{
    public async Task<Result<KeysetPageResponse<RoleAssignmentResponse>>> HandleAsync(
        ListRoleAssignmentsQuery query, CancellationToken cancellationToken)
    {
        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<RoleAssignmentResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var page = await repository.ListKeysetAsync(afterId, pageSize + 1, cancellationToken);
        var hasMore = page.Count > pageSize;
        var rows = page.Take(pageSize).ToArray();
        var items = rows.Select(r => new RoleAssignmentResponse(
            r.Id,
            r.UserId,
            r.Role.ToString(),
            r.PlatformTierId,
            r.PlatformTier?.Name,
            r.IsActive,
            r.GrantedByCredentialId,
            r.CreatedAt)).ToArray();
        var nextCursor = hasMore ? KeysetCursorCodec.EncodeLong(rows[^1].Id) : null;

        return Result<KeysetPageResponse<RoleAssignmentResponse>>.Success(
            new KeysetPageResponse<RoleAssignmentResponse>(items, nextCursor));
    }
}
