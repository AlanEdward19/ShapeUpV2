namespace ShapeUp.Features.Notifications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SendEmailHtml;
using SendEmailTemplate;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/notifications/emails")]
public sealed class NotificationsController : ControllerBase
{
    // Sends to an arbitrary "To" address with arbitrary content -- an internal/system utility, not
    // a self-service action for regular users (would be an open spam vector otherwise).
    [HttpPost("send-html")]
    [Authorize(Policy = "capability:platform.notifications.send")]
    public async Task<IActionResult> SendHtml(
        [FromBody] SendEmailHtmlCommand command,
        [FromServices] SendEmailHtmlHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => Accepted(success));
    }

    [HttpPost("send-template")]
    [Authorize(Policy = "capability:platform.notifications.send")]
    public async Task<IActionResult> SendTemplate(
        [FromBody] SendEmailTemplateCommand command,
        [FromServices] SendEmailTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => Accepted(success));
    }
}

