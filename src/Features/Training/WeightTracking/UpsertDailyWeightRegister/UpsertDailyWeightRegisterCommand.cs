namespace ShapeUp.Features.Training.WeightTracking.UpsertDailyWeightRegister;

public record UpsertDailyWeightRegisterCommand(decimal Weight, DateTime? DateUtc = null);
