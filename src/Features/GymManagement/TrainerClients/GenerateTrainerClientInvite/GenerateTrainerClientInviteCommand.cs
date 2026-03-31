namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

public record GenerateTrainerClientInviteCommand(
    int? TrainerPlanId,
    int? ExpiresInHours)
{
    private string ClientEmail { get; set; } = null!;
    private string TrainerName { get; set; } = null!;

    public void SetClientEmail(string clientEmail)
    {
        ClientEmail = clientEmail;
    }

    public void SetTrainerName(string trainerName)
    {
        TrainerName = trainerName;
    }

    public string GetClientEmail()
    {
        return ClientEmail;
    }
    
    public string GetTrainerName()
    {
        return TrainerName;
    }
}


