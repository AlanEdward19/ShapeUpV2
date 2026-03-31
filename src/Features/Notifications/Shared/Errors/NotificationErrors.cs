namespace ShapeUp.Features.Notifications.Shared.Errors;

using Microsoft.AspNetCore.Http;
using ShapeUp.Shared.Results;

public static class NotificationErrors
{
    public static Error ProviderConfigurationMissing(string settingName) =>
        new("notifications_provider_configuration_missing", $"Notification provider setting '{settingName}' is required.", StatusCodes.Status500InternalServerError);

    public static Error InvalidTemplateId(string templateId) =>
        CommonErrors.Validation($"TemplateId '{templateId}' is invalid.");

    public static Error ProviderFailure(string message, int statusCode = StatusCodes.Status502BadGateway) =>
        new("notifications_provider_error", message, statusCode);
}

