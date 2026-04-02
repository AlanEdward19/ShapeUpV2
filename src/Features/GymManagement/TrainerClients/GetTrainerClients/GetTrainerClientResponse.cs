namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;

using ShapeUp.Features.GymManagement.Shared.Entities;

public record GetTrainerClientResponse(
    int Id, 
    int TrainerId, 
    int ClientId, 
    string ClientName,
    string PlanName, 
    bool HasActivePlan,
    decimal AdherencePercentage,
    TrainerClientStatus Status,
    DateTime EnrolledAt);
