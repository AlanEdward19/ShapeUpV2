using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Equipments.GetEquipments;

public class GetEquipmentsHandler(IEquipmentRepository equipmentRepository)
{
    public async Task<Result<KeysetPageResponse<EquipmentResponse>>> HandleAsync(GetEquipmentsQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<EquipmentResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));

            lastId = (int)decoded;
        }

        var normalizedPageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var items = (await equipmentRepository.GetKeysetPageAsync(lastId, normalizedPageSize, cancellationToken))
            .Select(x => new EquipmentResponse(x.Id, x.Name, x.NamePt, x.Description))
            .ToArray();

        var nextCursor = items.Length < normalizedPageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<EquipmentResponse>>.Success(new KeysetPageResponse<EquipmentResponse>(items, nextCursor));
    }
}