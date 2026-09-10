using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Moderation.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;

public class GetPendingModerationsHandler(
    IFoodModerationRepository foodModerationRepository,
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<GetPendingModerationsQuery> validator)
{
    public async Task<Result<KeysetPageResponse<PendingModerationResponse>>> HandleAsync(
        GetPendingModerationsQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<KeysetPageResponse<PendingModerationResponse>>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        if (!string.IsNullOrWhiteSpace(query.Cursor) &&
            !KeysetCursorCodec.TryDecodeLong(query.Cursor, out _))
        {
            return Result<KeysetPageResponse<PendingModerationResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var (requests, nextCursor) = await foodModerationRepository.GetPendingAsync(pageSize, query.Cursor, cancellationToken);

        var items = new List<PendingModerationResponse>(requests.Count);
        foreach (var request in requests)
        {
            var food = await foodRepository.GetByIdAsync(request.FoodId, cancellationToken);
            var overrideDocument = await foodOverrideRepository.GetByIdAsync(request.FoodOverrideId, cancellationToken);
            if (food is null || overrideDocument is null)
                continue;

            items.Add(ToResponse(request, food, overrideDocument));
        }

        return Result<KeysetPageResponse<PendingModerationResponse>>.Success(
            new KeysetPageResponse<PendingModerationResponse>(items.ToArray(), nextCursor));
    }

    private static PendingModerationResponse ToResponse(
        FoodModerationRequestDocument request,
        FoodDocument food,
        FoodOverrideDocument overrideDocument) =>
        new(
            request.Id,
            food.Id,
            food.Name,
            request.RequestedByUserId,
            request.CreatedAtUtc,
            FoodMapper.ToMacroResponse(food.MacrosPer100),
            food.MicrosPer100 is null ? null : FoodMapper.ToMicroResponse(food.MicrosPer100),
            FoodMapper.ToMacroResponse(overrideDocument.MacrosPer100),
            overrideDocument.MicrosPer100 is null ? null : FoodMapper.ToMicroResponse(overrideDocument.MicrosPer100));
}
