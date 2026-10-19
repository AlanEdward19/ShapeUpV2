using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Shared.Invites;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClientInvites;

public record GetTrainerClientInvitesQuery(int TrainerId, string? Cursor, int? PageSize);

/// <summary>The trainer's invites in any status, newest first; keyset pagination on the invite Id (same shape as the nutritionist's).</summary>
public class GetTrainerClientInvitesHandler(
    ITrainerClientInviteRepository repository,
    TimeProvider? timeProvider = null)
{
    public async Task<Result<KeysetPageResponse<GetTrainerClientInviteResponse>>> HandleAsync(
        GetTrainerClientInvitesQuery query,
        CancellationToken cancellationToken)
    {
        int? beforeId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<GetTrainerClientInviteResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            beforeId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var page = await repository.ListAsync(query.TrainerId, beforeId, pageSize + 1, cancellationToken);

        var hasMore = page.Count > pageSize;
        var invites = page.Take(pageSize).ToArray();
        var items = invites
            .Select(invite => new GetTrainerClientInviteResponse(
                invite.Id,
                invite.InviteeEmail,
                InviteRules.StatusName(invite.Status.ToString(), invite.ExpiresAtUtc, nowUtc),
                invite.CreatedAtUtc,
                invite.ExpiresAtUtc,
                invite.AcceptedAtUtc))
            .ToArray();

        return Result<KeysetPageResponse<GetTrainerClientInviteResponse>>.Success(
            new KeysetPageResponse<GetTrainerClientInviteResponse>(items, hasMore ? KeysetCursorCodec.EncodeLong(invites[^1].Id) : null));
    }
}
