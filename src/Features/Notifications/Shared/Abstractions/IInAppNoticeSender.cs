namespace ShapeUp.Features.Notifications.Shared.Abstractions;

/// <summary>Writes an in-app notice into a user's inbox (read through <c>api/notifications/inbox</c>).</summary>
public interface IInAppNoticeSender
{
    Task SendAsync(int userId, string title, string body, CancellationToken cancellationToken);
}
