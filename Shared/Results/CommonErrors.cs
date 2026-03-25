namespace ShapeUp.Shared.Results;

using Microsoft.AspNetCore.Http;

public static class CommonErrors
{
    public static Error Validation(string message) =>
        new("validation_error", message, StatusCodes.Status400BadRequest);

    public static Error NotFound(string message) =>
        new("not_found", message, StatusCodes.Status404NotFound);

    public static Error Forbidden(string message) =>
        new("forbidden", message, StatusCodes.Status403Forbidden);

    public static Error Conflict(string message) =>
        new("conflict", message, StatusCodes.Status409Conflict);

    public static Error Unauthorized(string message) =>
        new("unauthorized", message, StatusCodes.Status401Unauthorized);
}

