namespace ShapeUp.Features.GymManagement.TrainerClients.Shared;

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShapeUp.Shared.Results;

public sealed class TrainerClientInvitePayloadCodec(IOptions<TrainerClientInviteRegisterUrlOptions> options) : ITrainerClientInvitePayloadCodec
{
    private readonly TrainerClientInviteRegisterUrlOptions _options = options.Value;

    public string Encode(TrainerClientInviteUrlPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var obfuscated = ApplyObfuscation(bytes);
        return ToBase64Url(obfuscated);
    }

    public Result<TrainerClientInviteUrlPayload> Decode(string encodedPayload)
    {
        if (string.IsNullOrWhiteSpace(encodedPayload))
            return Result<TrainerClientInviteUrlPayload>.Failure(CommonErrors.Validation("Invite payload is required."));

        try
        {
            var obfuscated = FromBase64Url(encodedPayload.Trim());
            var plainBytes = ReverseObfuscation(obfuscated);
            var payload = JsonSerializer.Deserialize<TrainerClientInviteUrlPayload>(plainBytes);
            if (payload is null || payload.TrainerId <= 0 || string.IsNullOrWhiteSpace(payload.InviteToken))
                return Result<TrainerClientInviteUrlPayload>.Failure(CommonErrors.Validation("Invite payload is invalid."));

            return Result<TrainerClientInviteUrlPayload>.Success(payload);
        }
        catch
        {
            return Result<TrainerClientInviteUrlPayload>.Failure(CommonErrors.Validation("Invite payload is invalid."));
        }
    }

    private byte[] ApplyObfuscation(ReadOnlySpan<byte> value)
    {
        var saltBytes = GetSaltBytes();
        var output = new byte[value.Length];

        for (var index = 0; index < value.Length; index++)
        {
            var shift = (index % 7) + 1;
            var shifted = (byte)((value[index] + shift) & 0xFF);
            output[index] = (byte)(shifted ^ saltBytes[index % saltBytes.Length]);
        }

        return output;
    }

    private byte[] ReverseObfuscation(ReadOnlySpan<byte> value)
    {
        var saltBytes = GetSaltBytes();
        var output = new byte[value.Length];

        for (var index = 0; index < value.Length; index++)
        {
            var shift = (index % 7) + 1;
            var xored = (byte)(value[index] ^ saltBytes[index % saltBytes.Length]);
            output[index] = (byte)((xored - shift) & 0xFF);
        }

        return output;
    }

    private byte[] GetSaltBytes()
    {
        var salt = string.IsNullOrWhiteSpace(_options.ObfuscationSalt)
            ? "shapeup-trainer-invite-v1"
            : _options.ObfuscationSalt;

        return Encoding.UTF8.GetBytes(salt);
    }

    private string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (base64.Length % 4);
        if (padding < 4)
            base64 = base64.PadRight(base64.Length + padding, '=');

        return Convert.FromBase64String(base64);
    }
}

