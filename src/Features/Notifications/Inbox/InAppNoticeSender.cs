using ShapeUp.Features.Notifications.Shared.Abstractions;

namespace ShapeUp.Features.Notifications.Inbox;

public class InAppNoticeSender(InboxDbContext db) : IInAppNoticeSender
{
    private const int TitleMax = 160;
    private const int BodyMax = 2000;

    public async Task SendAsync(int userId, string title, string body, CancellationToken cancellationToken)
    {
        db.Notices.Add(new InAppNotice
        {
            UserId = userId,
            Title = Truncate(title.Trim(), TitleMax),
            Body = Truncate(body.Trim(), BodyMax),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
