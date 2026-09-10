using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.SearchFoods;

public class SearchFoodsHandler(
    IFoodRepository foodRepository,
    IValidator<SearchFoodsQuery> validator)
{
    public async Task<Result<KeysetPageResponse<FoodResponse>>> HandleAsync(
        SearchFoodsQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<KeysetPageResponse<FoodResponse>>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        if (!string.IsNullOrWhiteSpace(query.Cursor) &&
            !KeysetCursorCodec.TryDecodeLong(query.Cursor, out _))
        {
            return Result<KeysetPageResponse<FoodResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var (items, nextCursor) = await foodRepository.SearchAsync(
            query.Query?.Trim() ?? string.Empty,
            pageSize,
            query.Cursor,
            cancellationToken);

        var responses = items.Select(FoodMapper.ToResponse).ToArray();
        return Result<KeysetPageResponse<FoodResponse>>.Success(new KeysetPageResponse<FoodResponse>(responses, nextCursor));
    }
}
