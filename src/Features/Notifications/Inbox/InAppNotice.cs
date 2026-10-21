using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Privacy;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Notifications.Inbox;

public class InAppNotice
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public class InboxDbContext(DbContextOptions<InboxDbContext> options) : DbContext(options)
{
    public DbSet<InAppNotice> Notices => Set<InAppNotice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var notice = modelBuilder.Entity<InAppNotice>();
        notice.HasKey(x => x.Id);
        notice.Property(x => x.Title).HasMaxLength(160).IsRequired();
        notice.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        notice.HasIndex(x => new { x.UserId, x.Id });
    }
}

public record NoticeResponse(int Id, int UserId, string Title, string Body, DateTime CreatedAtUtc, DateTime? ReadAtUtc);
public record SendNoticeCommand(int UserId, string Title, string Body);

public class SendNoticeValidator : AbstractValidator<SendNoticeCommand>
{
    public SendNoticeValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public record ListNoticesQuery(string? Cursor, int? PageSize);

public class ListNoticesHandler(InboxDbContext db)
{
    public async Task<Result<KeysetPageResponse<NoticeResponse>>> HandleAsync(
        ListNoticesQuery query, int userId, CancellationToken cancellationToken)
    {
        int? beforeId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded is <= 0 or > int.MaxValue)
                return Result<KeysetPageResponse<NoticeResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            beforeId = (int)decoded;
        }

        var size = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var rowsQuery = db.Notices.AsNoTracking().Where(n => n.UserId == userId);
        if (beforeId is int id)
            rowsQuery = rowsQuery.Where(n => n.Id < id);
        var page = await rowsQuery.OrderByDescending(n => n.Id).Take(size + 1).ToListAsync(cancellationToken);
        var hasMore = page.Count > size;
        var rows = page.Take(size).ToArray();
        return Result<KeysetPageResponse<NoticeResponse>>.Success(new KeysetPageResponse<NoticeResponse>(
            rows.Select(NoticeMapping.ToResponse).ToArray(), hasMore ? KeysetCursorCodec.EncodeLong(rows[^1].Id) : null));
    }
}

public record MarkNoticeReadCommand(int NoticeId);

public class MarkNoticeReadHandler(InboxDbContext db)
{
    public async Task<Result> HandleAsync(MarkNoticeReadCommand command, int userId, CancellationToken cancellationToken)
    {
        var updated = await db.Notices.Where(n => n.Id == command.NoticeId && n.UserId == userId && n.ReadAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAtUtc, DateTime.UtcNow), cancellationToken);
        if (updated == 0 && !await db.Notices.AnyAsync(n => n.Id == command.NoticeId && n.UserId == userId, cancellationToken))
            return Result.Failure(CommonErrors.NotFound("Notice was not found."));
        return Result.Success();
    }
}

public class SendNoticeHandler(InboxDbContext db, IUserRepository users, IValidator<SendNoticeCommand> validator)
{
    public async Task<Result<NoticeResponse>> HandleAsync(SendNoticeCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<NoticeResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));
        if (await users.GetByIdAsync(command.UserId, cancellationToken) is null)
            return Result<NoticeResponse>.Failure(CommonErrors.NotFound("User was not found."));

        var notice = new InAppNotice
        {
            UserId = command.UserId,
            Title = command.Title.Trim(),
            Body = command.Body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Notices.Add(notice);
        await db.SaveChangesAsync(cancellationToken);
        return Result<NoticeResponse>.Success(NoticeMapping.ToResponse(notice));
    }
}

static class NoticeMapping
{
    public static NoticeResponse ToResponse(InAppNotice notice) =>
        new(notice.Id, notice.UserId, notice.Title, notice.Body, notice.CreatedAtUtc, notice.ReadAtUtc);
}

[ApiController]
[Route("api/notifications/inbox")]
public class InboxController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "authenticated")]
    public async Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int? pageSize, [FromServices] ListNoticesHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new ListNoticesQuery(cursor, pageSize), HttpContext.GetUserId(), cancellationToken));

    [HttpPost("{id:int}/read")]
    [Authorize(Policy = "authenticated")]
    public async Task<IActionResult> Read(int id, [FromServices] MarkNoticeReadHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new MarkNoticeReadCommand(id), HttpContext.GetUserId(), cancellationToken));

    [HttpPost]
    [Authorize(Policy = "capability:platform.notifications.send")]
    public async Task<IActionResult> Send([FromBody] SendNoticeCommand command, [FromServices] SendNoticeHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(command, cancellationToken), success => Created("api/notifications/inbox", success));
}

public class InboxEraser(InboxDbContext db) : IUserDataEraser
{
    public string Module => "Inbox";

    public Task EraseAsync(int userId, string? email, CancellationToken cancellationToken) =>
        db.Notices.Where(n => n.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
