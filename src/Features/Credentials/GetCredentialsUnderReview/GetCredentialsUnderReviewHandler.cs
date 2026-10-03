namespace ShapeUp.Features.Credentials.GetCredentialsUnderReview;

using Shared.Abstractions;
using Shared.Entities;
using Shared.Lifecycle;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public record GetCredentialsUnderReviewQuery(string? Cursor, int? PageSize);

/// <summary>The review queue, oldest first, with each requester's name and e-mail. Keyset pagination on the credential Id.</summary>
public class GetCredentialsUnderReviewHandler(
    IProfessionalCredentialRepository repository,
    CredentialRequesterEnricher enricher)
{
    public async Task<Result<KeysetPageResponse<CredentialResponse>>> HandleAsync(
        GetCredentialsUnderReviewQuery query, CancellationToken cancellationToken)
    {
        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<CredentialResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var page = await repository.GetByStatusKeysetAsync(CredentialStatus.UnderReview, afterId, pageSize + 1, cancellationToken);

        var hasMore = page.Count > pageSize;
        var credentials = page.Take(pageSize).ToArray();
        var items = await enricher.ToResponsesAsync(credentials, cancellationToken);
        var nextCursor = hasMore ? KeysetCursorCodec.EncodeLong(credentials[^1].Id) : null;

        return Result<KeysetPageResponse<CredentialResponse>>.Success(new KeysetPageResponse<CredentialResponse>(items, nextCursor));
    }
}
