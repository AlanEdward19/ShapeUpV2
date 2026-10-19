namespace ShapeUp.Features.Nutrition.Clients.Shared;

/// <summary>Where the invite link points (Nutrition:ClientInvites:Link): a sign-up/landing page that opens the app when installed.</summary>
public sealed class NutritionInviteLinkOptions
{
    public const string SectionName = "Nutrition:ClientInvites:Link";

    public string BaseUrl { get; init; } = "https://shapeup.app/register";

    /// <summary>Query parameter that carries the token; the mobile app reads <c>nutrition</c>.</summary>
    public string TokenQueryParameter { get; init; } = "nutrition";

    public string EmailSubject { get; init; } = "{0} convidou você para acompanhar sua nutrição no ShapeUp";

    public string Build(string token)
    {
        var baseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? "https://shapeup.app/register" : BaseUrl.Trim();
        var separator = baseUrl.EndsWith('?') || baseUrl.EndsWith('&') ? string.Empty : baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}{Uri.EscapeDataString(TokenQueryParameter)}={Uri.EscapeDataString(token)}";
    }
}
