namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

using Microsoft.Extensions.Options;

public sealed class TrainerClientInviteRegisterUrlBuilder(
    IOptions<TrainerClientInviteRegisterUrlOptions> options,
    ITrainerClientInvitePayloadCodec payloadCodec) : ITrainerClientInviteRegisterUrlBuilder
{
    private readonly TrainerClientInviteRegisterUrlOptions _options = options.Value;

    public string BuildRegisterUrl(int trainerId, string inviteToken)
    {
        var payload = payloadCodec.Encode(new TrainerClientInviteUrlPayload(trainerId, inviteToken));
        var parameterName = string.IsNullOrWhiteSpace(_options.PayloadQueryParameterName)
            ? "payload"
            : _options.PayloadQueryParameterName.Trim();

        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://www.youtube.com/"
            : _options.BaseUrl.Trim();

        var separator = GetQuerySeparator(baseUrl);
        return $"{baseUrl}{separator}{Uri.EscapeDataString(parameterName)}={Uri.EscapeDataString(payload)}";
    }

    private string GetQuerySeparator(string baseUrl)
    {
        if (baseUrl.EndsWith('?') || baseUrl.EndsWith('&'))
            return string.Empty;

        return baseUrl.Contains('?') ? "&" : "?";
    }
}

