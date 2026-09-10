using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;
using ShapeUp.Features.Nutrition.Diary.GetDiaryDay;
using ShapeUp.Features.Nutrition.Diary.RemoveDiaryEntry;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary;

[ApiController]
[Route("api/nutrition/diary")]
public class DiaryController : ControllerBase
{
    [HttpPost("entries")]
    public async Task<IActionResult> AddEntry(
        [FromBody] AddDiaryEntryCommand command,
        [FromServices] AddDiaryEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("entries/{entryId}")]
    public async Task<IActionResult> RemoveEntry(
        string entryId,
        [FromQuery] DateOnly date,
        [FromServices] RemoveDiaryEntryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveDiaryEntryCommand(entryId, date), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDay(
        [FromQuery] DateOnly date,
        [FromServices] GetDiaryDayHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDiaryDayQuery(date), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
