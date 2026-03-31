namespace ShapeUp.Features.Notifications.Shared.Helpers;

using System.Text.Json;

public static class NotificationTemplateVariablesConverter
{
    public static IReadOnlyDictionary<string, object?> ToDictionary(Dictionary<string, JsonElement>? variables)
    {
        if (variables is null || variables.Count == 0)
            return new Dictionary<string, object?>();

        return variables.ToDictionary(item => item.Key, item => Convert(item.Value));
    }

    private static object? Convert(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var integerValue) => integerValue,
        JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(Convert).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(property => property.Name, property => Convert(property.Value)),
        _ => element.ToString()
    };
}

