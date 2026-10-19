namespace ShapeUp.Shared.Invites;

/// <summary>
/// The rules every professional invite follows, whatever the profession (nutritionist or trainer), so the two cannot drift:
/// how long an invite lives, how many may wait at once, what each state is called, and when a deadline has passed.
/// </summary>
public static class InviteRules
{
    public const int MinLifetimeHours = 1;
    public const int MaxLifetimeHours = 168;
    public const int DefaultLifetimeHours = 168;

    /// <summary>Invites that may wait (not used, not revoked, not expired) at once; deactivate one or wait for it to expire.</summary>
    public const int MaxWaitingInvites = 20;

    public const string Invited = "Invited";
    public const string Accepted = "Accepted";
    public const string Revoked = "Revoked";
    public const string Expired = "Expired";
    public const string Superseded = "Superseded";

    public static string LimitMessage =>
        $"You already have {MaxWaitingInvites} pending invites. Revoke one or wait for it to expire.";

    public static bool IsValidLifetime(int hours) => hours is >= MinLifetimeHours and <= MaxLifetimeHours;

    public static DateTime ExpiresAt(DateTime nowUtc, int? lifetimeHours) =>
        nowUtc.AddHours(lifetimeHours ?? DefaultLifetimeHours);

    /// <summary>The deadline is exclusive: at <c>ExpiresAtUtc == now</c> the invite is already expired.</summary>
    public static bool HasExpired(DateTime expiresAtUtc, DateTime nowUtc) => expiresAtUtc <= nowUtc;

    /// <summary>A waiting invite past its deadline is reported as Expired; every other state is reported as stored.</summary>
    public static string StatusName(string storedStatus, DateTime expiresAtUtc, DateTime nowUtc) =>
        storedStatus == Invited && HasExpired(expiresAtUtc, nowUtc) ? Expired : storedStatus;
}
