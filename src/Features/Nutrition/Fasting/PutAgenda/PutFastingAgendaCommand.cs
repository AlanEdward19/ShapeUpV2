namespace ShapeUp.Features.Nutrition.Fasting.PutAgenda;

public sealed record PutFastingAgendaCommand(
    string Protocol,
    int EatingStartMinutes,
    string TimeZone,
    int? FastHours);
