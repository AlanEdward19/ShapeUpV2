namespace ShapeUp.Features.Notifications;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using SendEmailHtml;
using SendEmailTemplate;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/notifications/emails")]
public sealed class NotificationsController : ControllerBase
{
    [HttpPost("send-html")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "notifications:emails:send_html" }])]
    public async Task<IActionResult> SendHtml(
        [FromBody] SendEmailHtmlCommand command,
        [FromServices] SendEmailHtmlHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => Accepted(success));
    }

    [HttpPost("send-template")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "notifications:emails:send_template" }])]
    public async Task<IActionResult> SendTemplate(
        [FromBody] SendEmailTemplateCommand command,
        [FromServices] SendEmailTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => Accepted(success));
    }
}

