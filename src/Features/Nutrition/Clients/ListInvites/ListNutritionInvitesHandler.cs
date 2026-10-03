using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ListInvites;

public record ListNutritionInvitesQuery(string? Cursor, int? PageSize);

/// <summary>The nutritionist's pending (not expired) invites, oldest first; keyset pagination on the invite Id.</summary>
public class ListNutritionInvitesHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientInviteRepository inviteRepository)
{
    public async Task<Result<KeysetPageResponse<NutritionInviteResponse>>> HandleAsync(
        ListNutritionInvitesQuery query,
        int nutritionistUserId,
        CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<KeysetPageResponse<NutritionInviteResponse>>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<NutritionInviteResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var page = await inviteRepository.ListPendingAsync(
            nutritionistUserId, NutritionAccessPolicy.RelationshipType, DateTime.UtcNow, afterId, pageSize + 1, cancellationToken);

        var hasMore = page.Count > pageSize;
        var invites = page.Take(pageSize).ToArray();
        var items = invites.Select(i => new NutritionInviteResponse(i.Id, i.CreatedAtUtc, i.ExpiresAtUtc)).ToArray();

        return Result<KeysetPageResponse<NutritionInviteResponse>>.Success(
            new KeysetPageResponse<NutritionInviteResponse>(items, hasMore ? KeysetCursorCodec.EncodeLong(invites[^1].Id) : null));
    }
}
