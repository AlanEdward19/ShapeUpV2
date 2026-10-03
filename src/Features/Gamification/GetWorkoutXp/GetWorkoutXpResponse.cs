namespace ShapeUp.Features.Gamification.GetWorkoutXp;

/// <summary><paramref name="Xp"/> é 0 quando o antifraude não concedeu crédito à sessão.</summary>
public record GetWorkoutXpResponse(string SessionId, int Xp, bool CreditGranted);
