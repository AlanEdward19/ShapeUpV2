namespace ShapeUp.Features.GymManagement.GymStaff.GetGymStaff;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetGymStaffHandler(IGymStaffRepository repository, IGymRepository gymRepository)
{
    public async Task<Result<KeysetPageResponse<GetGymStaffResponse>>> HandleAsync(GetGymStaffQuery query, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(query.GymId, cancellationToken);
        if (gym is null) return Result<KeysetPageResponse<GetGymStaffResponse>>.Failure(GymManagementErrors.GymNotFound(query.GymId));

        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetGymStaffResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var staff = await repository.GetByGymIdKeysetAsync(query.GymId, lastId, pageSize, cancellationToken);
        var items = staff.Select(s => new GetGymStaffResponse(s.Id, s.GymId, s.UserId, s.Role.ToString(), s.HiredAt)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetGymStaffResponse>>.Success(new KeysetPageResponse<GetGymStaffResponse>(items, nextCursor));
    }
}

