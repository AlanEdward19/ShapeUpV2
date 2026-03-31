namespace ShapeUp.Features.GymManagement.Shared.Security;

using System.Security.Cryptography;
using System.Text;

public static class TrainerClientInviteTokenCodec
{
    public static string GenerateToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string ComputeHash(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}


