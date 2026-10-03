namespace ShapeUp.Features.Credentials;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using GetCredentialsUnderReview;
using GetMyCredentials;
using ReviewCredential;
using SubmitCredential;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/credentials")]
public class CredentialsController : ControllerBase
{
    // Self-service: authentication alone is the gate.
    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitCredentialCommand command,
        [FromServices] SubmitCredentialHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetMine), null, success));
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromServices] GetMyCredentialsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    // "platform." capabilities require an active platform Admin role.
    [HttpGet("under-review")]
    [Authorize(Policy = "capability:platform.credentials.review")]
    public async Task<IActionResult> GetUnderReview(
        [FromServices] GetCredentialsUnderReviewHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = "capability:platform.credentials.review")]
    public async Task<IActionResult> Approve(
        int id,
        [FromServices] ReviewCredentialHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.ApproveAsync(id, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "capability:platform.credentials.review")]
    public async Task<IActionResult> Reject(
        int id,
        [FromBody] RejectCredentialCommand command,
        [FromServices] ReviewCredentialHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.RejectAsync(id, command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
