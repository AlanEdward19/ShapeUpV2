namespace ShapeUp.Features.Nutrition.WeightTracking.UpsertDailyWeightRegister;

public record UpsertDailyWeightRegisterCommand(decimal Weight, DateTime? DateUtc = null);
