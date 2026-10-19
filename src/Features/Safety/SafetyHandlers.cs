namespace ShapeUp.Features.Safety;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public record CreateReportCommand(string TargetType, string Reason, string? TargetId = null, int? TargetUserId = null, string? Details = null);

public class CreateReportValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.TargetType).Must(t => ReportTargets.All.Contains(t)).WithMessage($"TargetType must be one of: {string.Join(", ", ReportTargets.All)}.");
        RuleFor(x => x.Reason).Must(r => ReportReasons.All.Contains(r)).WithMessage($"Reason must be one of: {string.Join(", ", ReportReasons.All)}.");
        RuleFor(x => x.TargetId).MaximumLength(64);
        RuleFor(x => x.Details).MaximumLength(1000);
        RuleFor(x => x).Must(x => x.TargetUserId.HasValue || !string.IsNullOrWhiteSpace(x.TargetId))
            .WithName("Target").WithMessage("Inform TargetUserId or TargetId.");
    }
}

public record ResolveReportCommand(string Decision, string? Note = null);

public class ResolveReportValidator : AbstractValidator<ResolveReportCommand>
{
    public ResolveReportValidator()
    {
        RuleFor(x => x.Decision).Must(d => d is ReportStatuses.Resolved or ReportStatuses.Dismissed)
            .WithMessage("Decision must be Resolved or Dismissed.");
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class ReportsHandler(SafetyDbContext db, IValidator<CreateReportCommand> createValidator, IValidator<ResolveReportCommand> resolveValidator)
{
    /// <summary>Reports a user may file per day; keeps the review queue usable against spam.</summary>
    public const int DailyLimit = 30;

    public async Task<Result<ReportResponse>> CreateAsync(CreateReportCommand command, int reporterUserId, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<ReportResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        if (command.TargetUserId == reporterUserId)
            return Result<ReportResponse>.Failure(CommonErrors.Validation("You cannot report yourself."));

        var since = DateTime.UtcNow.AddDays(-1);
        if (await db.ContentReports.CountAsync(r => r.ReporterUserId == reporterUserId && r.CreatedAtUtc > since, ct) >= DailyLimit)
            return Result<ReportResponse>.Failure(CommonErrors.Conflict("Daily report limit reached. Try again tomorrow."));

        var report = new ContentReport
        {
            ReporterUserId = reporterUserId,
            TargetType = command.TargetType,
            TargetId = string.IsNullOrWhiteSpace(command.TargetId) ? null : command.TargetId.Trim(),
            TargetUserId = command.TargetUserId,
            Reason = command.Reason,
            Details = string.IsNullOrWhiteSpace(command.Details) ? null : command.Details.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.ContentReports.Add(report);
        await db.SaveChangesAsync(ct);
        return Result<ReportResponse>.Success(ToResponse(report, includeReporter: false));
    }

    /// <summary>Admin queue: oldest first, so nothing waits behind newer reports.</summary>
    public async Task<Result<KeysetPageResponse<ReportResponse>>> ListAsync(string? status, string? cursor, int? pageSize, CancellationToken ct)
    {
        int? afterId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(cursor, out var decoded) || decoded is < 0 or > int.MaxValue)
                return Result<KeysetPageResponse<ReportResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            afterId = (int)decoded;
        }

        var wanted = string.IsNullOrWhiteSpace(status) ? ReportStatuses.Open : status;
        if (wanted is not (ReportStatuses.Open or ReportStatuses.Resolved or ReportStatuses.Dismissed))
            return Result<KeysetPageResponse<ReportResponse>>.Failure(CommonErrors.Validation("Invalid status."));

        var size = new KeysetPageRequest(cursor, pageSize).NormalizePageSize();
        var page = await db.ContentReports.AsNoTracking()
            .Where(r => r.Status == wanted && (afterId == null || r.Id > afterId))
            .OrderBy(r => r.Id).Take(size + 1).ToListAsync(ct);

        var items = page.Take(size).Select(r => ToResponse(r, includeReporter: true)).ToArray();
        var next = page.Count > size ? KeysetCursorCodec.EncodeLong(items[^1].Id) : null;
        return Result<KeysetPageResponse<ReportResponse>>.Success(new KeysetPageResponse<ReportResponse>(items, next));
    }

    public async Task<Result<ReportResponse>> ResolveAsync(int reportId, ResolveReportCommand command, int adminUserId, CancellationToken ct)
    {
        var validation = await resolveValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<ReportResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var report = await db.ContentReports.FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report is null)
            return Result<ReportResponse>.Failure(CommonErrors.NotFound($"Report {reportId} was not found."));
        if (report.Status != ReportStatuses.Open)
            return Result<ReportResponse>.Failure(CommonErrors.Conflict("Report was already decided."));

        report.Status = command.Decision;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.ResolvedByUserId = adminUserId;
        report.ResolutionNote = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        await db.SaveChangesAsync(ct);
        return Result<ReportResponse>.Success(ToResponse(report, includeReporter: true));
    }

    private static ReportResponse ToResponse(ContentReport r, bool includeReporter) => new(
        r.Id, r.TargetType, r.TargetId, r.TargetUserId, r.Reason, r.Details, r.Status, r.CreatedAtUtc, r.ResolvedAtUtc, r.ResolutionNote,
        includeReporter ? r.ReporterUserId : null);
}

public class BlocksHandler(
    SafetyDbContext db,
    IUserRepository userRepository,
    IProfessionalClientRelationshipRepository relationships,
    ITrainerClientRepository trainerClients)
{
    public async Task<Result<BlockResponse>> BlockAsync(int blockerUserId, int blockedUserId, CancellationToken ct)
    {
        if (blockerUserId == blockedUserId)
            return Result<BlockResponse>.Failure(CommonErrors.Validation("You cannot block yourself."));

        var blocked = await userRepository.GetByIdAsync(blockedUserId, ct);
        if (blocked is null)
            return Result<BlockResponse>.Failure(CommonErrors.NotFound("User not found."));

        var block = await db.UserBlocks.FirstOrDefaultAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, ct);
        if (block is null)
        {
            block = new UserBlock { BlockerUserId = blockerUserId, BlockedUserId = blockedUserId, CreatedAtUtc = DateTime.UtcNow };
            db.UserBlocks.Add(block);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // A concurrent identical request won; the block exists either way.
                db.Entry(block).State = EntityState.Detached;
                block = await db.UserBlocks.AsNoTracking().FirstAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, ct);
            }
        }

        await EndLinksAsync(blockerUserId, blockedUserId, ct);
        return Result<BlockResponse>.Success(new BlockResponse(blockedUserId, blocked.DisplayName, block.CreatedAtUtc));
    }

    public async Task<Result> UnblockAsync(int blockerUserId, int blockedUserId, CancellationToken ct)
    {
        await db.UserBlocks.Where(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId).ExecuteDeleteAsync(ct);
        return Result.Success();
    }

    public async Task<Result<BlockResponse[]>> ListAsync(int blockerUserId, CancellationToken ct)
    {
        var blocks = await db.UserBlocks.AsNoTracking()
            .Where(b => b.BlockerUserId == blockerUserId).OrderByDescending(b => b.Id).Take(500).ToListAsync(ct);
        var users = (await userRepository.GetByIdsAsync(blocks.Select(b => b.BlockedUserId).ToArray(), ct)).ToDictionary(u => u.Id);
        return Result<BlockResponse[]>.Success(blocks
            .Select(b => new BlockResponse(b.BlockedUserId, users.GetValueOrDefault(b.BlockedUserId)?.DisplayName, b.CreatedAtUtc))
            .ToArray());
    }

    /// <summary>Blocking ends the professional links between the two, in either direction (nutrition and trainer).</summary>
    private async Task EndLinksAsync(int a, int b, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        foreach (var (pro, client) in new[] { (a, b), (b, a) })
        {
            await relationships.EndActiveAsync(pro, client, NutritionAccessPolicy.RelationshipType, now, ct);
            if (await trainerClients.GetByTrainerAndClientAsync(pro, client, ct) is { IsActive: true })
                await trainerClients.UnassignAsync(pro, client, ct);
        }
    }
}

public class UserBlockLookup(SafetyDbContext db) : IUserBlockLookup
{
    public Task<bool> IsBlockedEitherWayAsync(int userA, int userB, CancellationToken cancellationToken) =>
        db.UserBlocks.AsNoTracking().AnyAsync(b =>
            (b.BlockerUserId == userA && b.BlockedUserId == userB) || (b.BlockerUserId == userB && b.BlockedUserId == userA), cancellationToken);
}

/// <summary>Account deletion: the user's blocks (either side) and reports filed by them go; reports about them stay as moderation history, anonymized.</summary>
public class SafetyUserDataEraser(SafetyDbContext db) : ShapeUp.Shared.Privacy.IUserDataEraser
{
    public string Module => "Safety";

    public async Task EraseAsync(int userId, string? email, CancellationToken cancellationToken)
    {
        await db.UserBlocks.Where(b => b.BlockerUserId == userId || b.BlockedUserId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.ContentReports.Where(r => r.ReporterUserId == userId && r.Status == ReportStatuses.Open).ExecuteDeleteAsync(cancellationToken);
        await db.ContentReports.Where(r => r.ReporterUserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ReporterUserId, 0).SetProperty(r => r.Details, (string?)null), cancellationToken);
    }
}
