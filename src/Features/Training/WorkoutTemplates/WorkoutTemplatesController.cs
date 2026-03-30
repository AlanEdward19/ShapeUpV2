using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.CopyWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.CreateWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.DeleteWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplateById;
using ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplates;
using ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates;

[ApiController]
[Route("api/training/workout-templates")]
public class WorkoutTemplatesController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkoutTemplateCommand command,
        [FromServices] CreateWorkoutTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { templateId = success.TemplateId }, success));
    }

    [HttpPost("{templateId}/copy")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:copy" }])]
    public async Task<IActionResult> Copy(
        string templateId,
        [FromBody] CopyWorkoutTemplateCommand command,
        [FromServices] CopyWorkoutTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { TemplateId = templateId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { templateId = success.TemplateId }, success));
    }

    [HttpPost("{templateId}/assign/{targetUserId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:assign" }])]
    public async Task<IActionResult> Assign(
        string templateId,
        int targetUserId,
        [FromBody] AssignWorkoutTemplateCommand command,
        [FromServices] AssignWorkoutTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var request = command with { TemplateId = templateId, TargetUserId = targetUserId };
        var result = await handler.HandleAsync(request, HttpContext.GetUserId(), HttpContext.GetUserScopes(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction("GetById", "WorkoutPlans", new { planId = success.PlanId }, success));
    }

    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:read" }])]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetWorkoutTemplatesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutTemplatesQuery(cursor, pageSize), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{templateId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:read" }])]
    public async Task<IActionResult> GetById(
        string templateId,
        [FromServices] GetWorkoutTemplateByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutTemplateByIdQuery(templateId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{templateId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:update" }])]
    public async Task<IActionResult> Update(
        string templateId,
        [FromBody] UpdateWorkoutTemplateCommand command,
        [FromServices] UpdateWorkoutTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { TemplateId = templateId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => Ok(success));
    }

    [HttpDelete("{templateId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-templates:delete" }])]
    public async Task<IActionResult> Delete(
        string templateId,
        [FromServices] DeleteWorkoutTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteWorkoutTemplateCommand(templateId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}

