namespace ShapeUp.Features.Credentials.GetVerifiedProfessionals;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Lifecycle;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public record GetVerifiedProfessionalsQuery(string? ProfessionType, string? Cursor, int? PageSize);

/// <summary>Public directory of verified, unexpired professional credentials. Keyset on credential Id.</summary>
public class GetVerifiedProfessionalsHandler(
    IProfessionalCredentialRepository repository,
    CredentialRequesterEnricher enricher,
    IValidator<GetVerifiedProfessionalsQuery> validator)
{
    public async Task<Result<KeysetPageResponse<CredentialResponse>>> HandleAsync(
        GetVerifiedProfessionalsQuery query, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<KeysetPageResponse<CredentialResponse>>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));

        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<CredentialResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var profession = string.IsNullOrWhiteSpace(query.ProfessionType) ? null : query.ProfessionType.Trim();

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var page = await repository.GetVerifiedDirectoryKeysetAsync(
            profession, afterId, pageSize + 1, DateTime.UtcNow, cancellationToken);
        var hasMore = page.Count > pageSize;
        var credentials = page.Take(pageSize).ToArray();
        var items = await enricher.ToResponsesAsync(credentials, cancellationToken);
        var nextCursor = hasMore ? KeysetCursorCodec.EncodeLong(credentials[^1].Id) : null;

        return Result<KeysetPageResponse<CredentialResponse>>.Success(
            new KeysetPageResponse<CredentialResponse>(items, nextCursor));
    }
}
