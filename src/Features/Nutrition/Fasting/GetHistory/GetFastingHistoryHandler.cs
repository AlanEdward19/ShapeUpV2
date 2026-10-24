using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.GetHistory;

public sealed class GetFastingHistoryHandler(
    NutritionDbContext dbContext,
    IValidator<GetFastingHistoryQuery> validator)
{
    public async Task<Result<KeysetPageResponse<FastingHistoryItemDto>>> HandleAsync(
        GetFastingHistoryQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<KeysetPageResponse<FastingHistoryItemDto>>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        DateTime? cursorCompletedAtUtc = null;
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!FastingHistoryCursorCodec.TryDecode(query.Cursor, out var decodedCompletedAt, out var decodedId))
                return Result<KeysetPageResponse<FastingHistoryItemDto>>.Failure(
                    CommonErrors.Validation("Invalid cursor."));

            cursorCompletedAtUtc = decodedCompletedAt;
            cursorId = decodedId;
        }

        var pageSize = query.PageSize is null or <= 0
            ? GetFastingHistoryQueryValidator.DefaultPageSize
            : Math.Min(query.PageSize.Value, GetFastingHistoryQueryValidator.MaxPageSize);

        var historyQuery = dbContext.FastingOverrides
            .AsNoTracking()
            .Where(o => o.UserId == actorUserId
                && (o.EndedEarlyAtUtc != null
                    || ((o.Status == FastingOverride.StatusCompleted || o.Status == FastingOverride.StatusCancelled)
                        && o.CompletedAtUtc != null)));

        if (cursorCompletedAtUtc is not null && cursorId is not null)
        {
            historyQuery = historyQuery.Where(o =>
                (o.EndedEarlyAtUtc ?? o.CompletedAtUtc)!.Value < cursorCompletedAtUtc.Value
                || ((o.EndedEarlyAtUtc ?? o.CompletedAtUtc)!.Value == cursorCompletedAtUtc.Value && o.Id.CompareTo(cursorId.Value) < 0));
        }

        var rows = await historyQuery
            .OrderByDescending(o => o.EndedEarlyAtUtc ?? o.CompletedAtUtc)
            .ThenByDescending(o => o.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(o => new FastingHistoryItemDto(
                o.Id,
                o.StartedAtUtc,
                o.Protocol,
                ComputeOutcome(o),
                ComputeFastingDurationSeconds(o),
                (o.EndedEarlyAtUtc ?? o.CompletedAtUtc)!.Value,
                o.FastHours,
                o.EndedEarlyAtUtc is null ? null : o.EndReason))
            .ToArray();

        string? nextCursor = null;
        if (items.Length == pageSize)
        {
            var last = rows[^1];
            nextCursor = FastingHistoryCursorCodec.Encode((last.EndedEarlyAtUtc ?? last.CompletedAtUtc)!.Value, last.Id);
        }

        return Result<KeysetPageResponse<FastingHistoryItemDto>>.Success(
            new KeysetPageResponse<FastingHistoryItemDto>(items, nextCursor));
    }

    internal static string ComputeOutcome(FastingOverride o) =>
        o.EndedEarlyAtUtc is not null ? "EndedEarly" : o.Status;

    internal static int ComputeFastingDurationSeconds(FastingOverride o)
    {
        if (o.EndedEarlyAtUtc is not null)
            return (int)Math.Max(0, (o.EndedEarlyAtUtc.Value - o.StartedAtUtc).TotalSeconds);

        var fastEndUtc = o.Status == FastingOverride.StatusCancelled
            && o.CompletedAtUtc is not null
            && o.CompletedAtUtc.Value < o.FastEndsAtUtc
            ? o.CompletedAtUtc.Value
            : o.FastEndsAtUtc;

        return (int)Math.Max(0, (fastEndUtc - o.StartedAtUtc).TotalSeconds);
    }
}
