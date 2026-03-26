namespace ShapeUp.Features.Authorization.Scopes.GetScopes;

using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetScopesHandler(IScopeRepository scopeRepository)
{
    public async Task<Result<KeysetPageResponse<GetScopesResponse>>> HandleAsync(
        string? cursor,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        int? lastScopeId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(cursor, out var decoded) || decoded is <= 0 or > int.MaxValue)
                return Result<KeysetPageResponse<GetScopesResponse>>.Failure(CommonErrors.Validation("Invalid keyset cursor."));

            lastScopeId = (int)decoded;
        }

        var request = new KeysetPageRequest(cursor, pageSize);
        var normalizedPageSize = request.NormalizePageSize();

        var scopes = await scopeRepository.GetAllKeysetAsync(lastScopeId, normalizedPageSize, cancellationToken);
        var items = scopes
            .Select(s => new GetScopesResponse(s.Id, s.Name, s.Domain, s.Subdomain, s.Action, s.Description))
            .ToArray();

        var nextCursor = items.Length < normalizedPageSize
            ? null
            : KeysetCursorCodec.EncodeLong(items[^1].ScopeId);

        return Result<KeysetPageResponse<GetScopesResponse>>.Success(new KeysetPageResponse<GetScopesResponse>(items, nextCursor));
    }
}
