using ShapeUp.Features.GymManagement.Shared;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Training.Shared.Abstractions;

namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;

using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetTrainerClientsHandler(
    ITrainerClientRepository repository,
    IWorkoutSessionRepository workoutSessionRepository,
    IWorkoutPlanRepository workoutPlanRepository)
{
    public async Task<Result<KeysetPageResponse<GetTrainerClientResponse>>> HandleAsync(
        GetTrainerClientsQuery query, 
        CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetTrainerClientResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var clientsData = await repository.GetByTrainerIdKeysetWithUserDataAsync(query.TrainerId, lastId, pageSize, cancellationToken);

        var items = new List<GetTrainerClientResponse>();
        
        foreach (var clientData in clientsData)
        {
            // Calcular adesão: obter sessões completadas desde EnrolledAt
            var completedSessions = await workoutSessionRepository.GetCompletedByUserInRangeAsync(
                clientData.ClientId,
                clientData.EnrolledAt,
                DateTime.UtcNow.AddDays(1), // Até amanhã para incluir hoje
                cancellationToken);

            // Determinar status baseado em IsActive
            var status = clientData.IsActive ? TrainerClientStatus.Active : TrainerClientStatus.Inactive;

            // Determinar se tem plano ativo
            var hasActivePlan = clientData.TrainerPlanId.HasValue;

            // Calcular adesão sofisticada baseada em séries executadas vs. prescritas
            decimal adherencePercentage = 0m;
            
            if (completedSessions.Count > 0)
            {
                // Carregar planos para as sessões
                var planIds = completedSessions
                    .Where(s => !string.IsNullOrEmpty(s.WorkoutPlanId))
                    .Select(s => s.WorkoutPlanId!)
                    .Distinct()
                    .ToList();

                var workoutPlans = new Dictionary<string, dynamic>();
                
                foreach (var planId in planIds)
                {
                    try
                    {
                        var plan = await workoutPlanRepository.GetByIdAsync(planId, cancellationToken);
                        if (plan != null)
                            workoutPlans[planId] = plan;
                    }
                    catch
                    {
                        // Se não conseguir carregar o plano, continua sem ele
                    }
                }

                // Calcular adesão: total de séries executadas / total de séries prescritas
                int totalSetsExecuted = 0;
                int totalSetsPrescribed = 0;

                foreach (var session in completedSessions)
                {
                    if (!session.IsCompleted || session.IsCancelled)
                        continue;

                    // Contar sets executados (excluindo extras)
                    var executedSets = session.Exercises
                        .SelectMany(e => e.Sets)
                        .Where(s => !s.IsExtra)
                        .Count();
                    
                    totalSetsExecuted += executedSets;

                    // Contar sets prescritos do plano
                    if (!string.IsNullOrEmpty(session.WorkoutPlanId) && 
                        workoutPlans.TryGetValue(session.WorkoutPlanId, out var planObj))
                    {
                        var plan = (ShapeUp.Features.Training.Shared.Documents.WorkoutPlanDocument)planObj;
                        var prescribedSets = plan.Exercises
                            .SelectMany(e => e.Sets)
                            .Count();
                        
                        totalSetsPrescribed += prescribedSets;
                    }
                    else
                    {
                        // Sem plano: assumir que o executado era esperado
                        totalSetsPrescribed += executedSets;
                    }
                }

                // Calcular percentual final
                if (totalSetsPrescribed > 0)
                {
                    adherencePercentage = (decimal)totalSetsExecuted / totalSetsPrescribed * 100m;
                    adherencePercentage = Math.Min(100m, Math.Max(0m, adherencePercentage));
                }
                else
                {
                    // Fallback: se não há prescrição mas completou sessões, considerar 100%
                    adherencePercentage = completedSessions.Count > 0 ? 100m : 0m;
                }
            }

            items.Add(new GetTrainerClientResponse(
                clientData.Id,
                clientData.TrainerId,
                clientData.ClientId,
                clientData.ClientName,
                clientData.PlanName ?? string.Empty,
                hasActivePlan,
                adherencePercentage,
                status,
                clientData.EnrolledAt));
        }

        var nextCursor = items.Count < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetTrainerClientResponse>>.Success(
            new KeysetPageResponse<GetTrainerClientResponse>(items.ToArray(), nextCursor));
    }
}
