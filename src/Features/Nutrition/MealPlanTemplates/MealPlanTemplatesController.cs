using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.MealPlanTemplates.AssignMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.DeleteMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplateById;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplates;
using ShapeUp.Features.Nutrition.MealPlanTemplates.UpdateMealPlanTemplate;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates;

/// <summary>Reusable meal plans of the nutritionist (same shape as Training workout templates).</summary>
[ApiController]
[Route("api/nutrition/meal-plan-templates")]
public class MealPlanTemplatesController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveMealPlanTemplateCommand command,
        [FromServices] CreateMealPlanTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { templateId = success.Id }, success));
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromServices] GetMealPlanTemplatesHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    [HttpGet("{templateId}")]
    public async Task<IActionResult> GetById(
        string templateId,
        [FromServices] GetMealPlanTemplateByIdHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(templateId, HttpContext.GetUserId(), cancellationToken));

    [HttpPut("{templateId}")]
    public async Task<IActionResult> Update(
        string templateId,
        [FromBody] SaveMealPlanTemplateCommand command,
        [FromServices] UpdateMealPlanTemplateHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(templateId, command, HttpContext.GetUserId(), cancellationToken));

    [HttpDelete("{templateId}")]
    public async Task<IActionResult> Delete(
        string templateId,
        [FromServices] DeleteMealPlanTemplateHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(templateId, HttpContext.GetUserId(), cancellationToken));

    /// <summary>Creates an inactive meal plan for the client from the template (201 with the new plan).</summary>
    [HttpPost("{templateId}/assign/{targetUserId:int}")]
    public async Task<IActionResult> Assign(
        string templateId,
        int targetUserId,
        [FromBody] AssignMealPlanTemplateCommand? command,
        [FromServices] AssignMealPlanTemplateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            templateId, targetUserId, command ?? new AssignMealPlanTemplateCommand(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => Created($"api/nutrition/users/{targetUserId}/meal-plans/{success.Id}", success));
    }
}
