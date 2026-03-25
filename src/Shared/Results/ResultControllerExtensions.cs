namespace ShapeUp.Shared.Results;

using Microsoft.AspNetCore.Mvc;

public static class ResultControllerExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
            return controller.Ok();

        var error = result.Error!;
        return controller.StatusCode(error.StatusCode, new { error.Code, error.Message });
    }

    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            if (result.Value is null)
                return controller.Ok();

            return onSuccess is null
                ? controller.Ok(result.Value)
                : onSuccess(result.Value);
        }

        var error = result.Error!;
        return controller.StatusCode(error.StatusCode, new { error.Code, error.Message });
    }
}

