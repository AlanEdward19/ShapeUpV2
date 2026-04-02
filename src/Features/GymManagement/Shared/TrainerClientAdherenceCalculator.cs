namespace ShapeUp.Features.GymManagement.Shared;

using ShapeUp.Features.Training.Shared.Documents;

/// <summary>
/// Calcula o percentual de adesão do cliente baseado em:
/// 1. Sessões completadas vs. esperadas
/// 2. Séries executadas vs. prescritas em cada sessão
/// 3. Se pulou exercícios ou séries
/// 
/// Fórmula: (total_series_executadas / total_series_prescritas) * 100
/// </summary>
public class TrainerClientAdherenceCalculator
{
    /// <summary>
    /// Calcula adesão baseada em sessões completadas e suas séries executadas.
    /// Compara o total de séries executadas com o total de séries esperadas.
    /// </summary>
    /// <param name="completedSessions">Lista de sessões completadas do cliente</param>
    /// <param name="workoutPlans">Dicionário com mapas de plano para sessão (WorkoutPlanId -> WorkoutPlanDocument)</param>
    /// <returns>Percentual de adesão (0-100)</returns>
    public static decimal CalculateAdherenceFromSessions(
        IReadOnlyList<WorkoutSessionDocument> completedSessions,
        Dictionary<string, WorkoutPlanDocument> workoutPlans)
    {
        if (completedSessions.Count == 0)
            return 0m;

        int totalSetsExecuted = 0;
        int totalSetsPrescribed = 0;

        foreach (var session in completedSessions)
        {
            if (!session.IsCompleted || session.IsCancelled)
                continue;

            // Contar sets executados
            var executedSets = session.Exercises
                .SelectMany(e => e.Sets)
                .Where(s => !s.IsExtra) // Não contar sets extras na aderência básica
                .Count();
            
            totalSetsExecuted += executedSets;

            // Contar sets prescritos (do plano)
            if (!string.IsNullOrEmpty(session.WorkoutPlanId) && 
                workoutPlans.TryGetValue(session.WorkoutPlanId, out var plan))
            {
                var prescribedSets = plan.Exercises
                    .SelectMany(e => e.Sets)
                    .Count();
                
                totalSetsPrescribed += prescribedSets;
            }
            else
            {
                // Se não temos o plano, assumir que o que foi executado era esperado
                // (usar como fallback)
                totalSetsPrescribed += executedSets;
            }
        }

        if (totalSetsPrescribed == 0)
            return completedSessions.Count > 0 ? 50m : 0m; // Fallback se sem prescrição

        var adherence = (decimal)totalSetsExecuted / totalSetsPrescribed * 100m;
        return Math.Min(100m, Math.Max(0m, adherence));
    }

    /// <summary>
    /// Calcula adesão por sessão (mais detalhado).
    /// Retorna um percentual para cada sessão baseado nas séries executadas.
    /// </summary>
    public static Dictionary<string, decimal> CalculateAdherencePerSession(
        IReadOnlyList<WorkoutSessionDocument> completedSessions,
        Dictionary<string, WorkoutPlanDocument> workoutPlans)
    {
        var adheranceBySession = new Dictionary<string, decimal>();

        foreach (var session in completedSessions)
        {
            if (!session.IsCompleted || session.IsCancelled)
                continue;

            var executedSets = session.Exercises
                .SelectMany(e => e.Sets)
                .Where(s => !s.IsExtra)
                .Count();

            int prescribedSets = 0;
            if (!string.IsNullOrEmpty(session.WorkoutPlanId) && 
                workoutPlans.TryGetValue(session.WorkoutPlanId, out var plan))
            {
                prescribedSets = plan.Exercises
                    .SelectMany(e => e.Sets)
                    .Count();
            }
            else
            {
                prescribedSets = executedSets;
            }

            var sessionAdherence = prescribedSets > 0 
                ? (decimal)executedSets / prescribedSets * 100m 
                : (executedSets > 0 ? 50m : 0m);

            adheranceBySession[session.Id] = Math.Min(100m, Math.Max(0m, sessionAdherence));
        }

        return adheranceBySession;
    }

    /// <summary>
    /// Calcula adesão por exercício (detalhe máximo).
    /// Retorna um percentual para cada exercício em cada sessão.
    /// </summary>
    public static decimal CalculateExerciseAdherence(
        WorkoutSessionDocument session,
        WorkoutPlanDocument? plan = null)
    {
        if (!session.IsCompleted || session.IsCancelled || session.Exercises.Count == 0)
            return 0m;

        var executedExercises = session.Exercises.Count;
        var prescribedExercises = plan?.Exercises.Count ?? executedExercises;

        // Métrica de exercícios completados
        var exerciseAdherence = (decimal)executedExercises / prescribedExercises * 100m;

        // Agora checar sets por exercício
        int totalSetsExecuted = 0;
        int totalSetsPrescribed = 0;

        // Executados
        foreach (var exercise in session.Exercises)
        {
            totalSetsExecuted += exercise.Sets.Count(s => !s.IsExtra);
        }

        // Prescritos
        if (plan != null)
        {
            foreach (var exercise in plan.Exercises)
            {
                totalSetsPrescribed += exercise.Sets.Count;
            }
        }
        else
        {
            totalSetsPrescribed = totalSetsExecuted;
        }

        // Adesão final: média entre exercícios completados e sets completados
        var setsAdherence = totalSetsPrescribed > 0
            ? (decimal)totalSetsExecuted / totalSetsPrescribed * 100m
            : (totalSetsExecuted > 0 ? 50m : 0m);

        return Math.Min(100m, Math.Max(0m, setsAdherence));
    }

    /// <summary>
    /// Versão simplificada: calcula apenas baseado em contagem de sessões.
    /// Se tem sessões completadas, retorna 100%; caso contrário, 0%.
    /// (Mantido para compatibilidade com versão anterior)
    /// </summary>
    public static decimal CalculateAdherenceFromCompletedCount(int completedSessionsCount)
    {
        return completedSessionsCount > 0 ? 100m : 0m;
    }
}


