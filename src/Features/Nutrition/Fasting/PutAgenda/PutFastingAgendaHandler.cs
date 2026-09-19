using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.PutAgenda;

public sealed class PutFastingAgendaHandler(
    NutritionDbContext dbContext,
    IUtcClock utcClock,
    IValidator<PutFastingAgendaCommand> validator)
{
    public async Task<Result<PutFastingAgendaResponse>> HandleAsync(
        PutFastingAgendaCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<PutFastingAgendaResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var hours = FastingClockCalculator.ParseProtocol(command.Protocol, command.FastHours);
        if (hours is null)
            return Result<PutFastingAgendaResponse>.Failure(CommonErrors.Validation("Protocol is invalid."));

        var (fastHours, eatHours) = hours.Value;
        var protocol = FastingClockCalculator.ParsePresetProtocol(command.Protocol) is not null
            ? command.Protocol
            : "custom";
        var nowUtc = utcClock.UtcNow;

        var agenda = await dbContext.FastingAgendas.FirstOrDefaultAsync(a => a.UserId == actorUserId, cancellationToken);
        if (agenda is null)
        {
            agenda = new FastingAgenda { UserId = actorUserId };
            dbContext.FastingAgendas.Add(agenda);
        }

        agenda.FastHours = fastHours;
        agenda.EatHours = eatHours;
        agenda.Protocol = protocol;
        agenda.EatingStartMinutes = command.EatingStartMinutes;
        agenda.TimeZone = command.TimeZone.Trim();
        agenda.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PutFastingAgendaResponse>.Success(new PutFastingAgendaResponse(FastingMapper.ToAgendaDto(agenda)));
    }
}
