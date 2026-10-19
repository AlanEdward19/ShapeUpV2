using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Invites;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.ListInvites;

public record ListNutritionInvitesQuery(string? Cursor, int? PageSize);

/// <summary>The nutritionist's invites in any status, newest first; keyset pagination on the invite Id.</summary>
public class ListNutritionInvitesHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientInviteRepository inviteRepository,
    TimeProvider? timeProvider = null)
{
    public async Task<Result<KeysetPageResponse<NutritionInviteResponse>>> HandleAsync(
        ListNutritionInvitesQuery query,
        int nutritionistUserId,
        CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<KeysetPageResponse<NutritionInviteResponse>>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        int? beforeId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<NutritionInviteResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            beforeId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var page = await inviteRepository.ListAsync(
            nutritionistUserId, NutritionAccessPolicy.RelationshipType, beforeId, pageSize + 1, cancellationToken);

        var hasMore = page.Count > pageSize;
        var invites = page.Take(pageSize).ToArray();
        var items = invites
            .Select(i => new NutritionInviteResponse(i.Id, i.CreatedAtUtc, i.ExpiresAtUtc, StatusOf(i, nowUtc), i.InviteeEmail))
            .ToArray();

        return Result<KeysetPageResponse<NutritionInviteResponse>>.Success(
            new KeysetPageResponse<NutritionInviteResponse>(items, hasMore ? KeysetCursorCodec.EncodeLong(invites[^1].Id) : null));
    }

    /// <summary>
    /// Same vocabulary as the trainer's invites (<see cref="InviteRules"/>): Pending is reported as Invited, and one whose
    /// deadline has passed as Expired. Nothing is rewritten in the database.
    /// </summary>
    public static string StatusOf(ProfessionalClientInvite invite, DateTime nowUtc) =>
        InviteRules.StatusName(
            invite.Status == ProfessionalClientInviteStatus.Pending ? InviteRules.Invited : invite.Status.ToString(),
            invite.ExpiresAtUtc,
            nowUtc);
}
