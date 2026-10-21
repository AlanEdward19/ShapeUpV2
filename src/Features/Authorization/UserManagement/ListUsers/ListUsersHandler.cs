using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement.ListUsers;

public class ListUsersHandler(IUserRepository userRepository)
{
    public async Task<Result<KeysetPageResponse<ListedUserResponse>>> HandleAsync(
        ListUsersQuery query, CancellationToken cancellationToken)
    {
        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<ListedUserResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var page = await userRepository.SearchKeysetAsync(query.Search, afterId, pageSize + 1, cancellationToken);
        var hasMore = page.Count > pageSize;
        var users = page.Take(pageSize).ToArray();
        var items = users
            .Select(u => new ListedUserResponse(u.Id, u.Email, u.DisplayName, u.IsActive, u.CreatedAt))
            .ToArray();
        var nextCursor = hasMore ? KeysetCursorCodec.EncodeLong(users[^1].Id) : null;

        return Result<KeysetPageResponse<ListedUserResponse>>.Success(
            new KeysetPageResponse<ListedUserResponse>(items, nextCursor));
    }
}
