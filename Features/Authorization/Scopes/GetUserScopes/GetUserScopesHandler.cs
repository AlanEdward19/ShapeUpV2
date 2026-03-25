namespace ShapeUp.Features.Authorization.Scopes.GetUserScopes;

using Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetUserScopesHandler(IScopeRepository scopeRepository)
{
    public async Task<Result<KeysetPageResponse<GetUserScopesResponse>>> HandleAsync(
        int userId,
        string? cursor,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        int? lastScopeId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(cursor, out var decoded) || decoded is <= 0 or > int.MaxValue)
            {
                return Result<KeysetPageResponse<GetUserScopesResponse>>.Failure(
                    CommonErrors.Validation("Invalid keyset cursor."));
            }

            lastScopeId = (int)decoded;
        }

        var request = new KeysetPageRequest(cursor, pageSize);
        var normalizedPageSize = request.NormalizePageSize();

        var scopes = await scopeRepository.GetUserScopesKeysetAsync(userId, lastScopeId, normalizedPageSize, cancellationToken);
        var items = scopes
            .Select(s => new GetUserScopesResponse(s.Id, s.Name))
            .ToArray();

        var nextCursor = items.Length < normalizedPageSize || scopes.Count == 0
            ? null
            : KeysetCursorCodec.EncodeLong(scopes[^1].Id);

        return Result<KeysetPageResponse<GetUserScopesResponse>>.Success(new KeysetPageResponse<GetUserScopesResponse>(items, nextCursor));
    }
}
