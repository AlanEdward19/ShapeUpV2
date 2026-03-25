namespace ShapeUp.Features.GymManagement.Gyms.GetGyms;

public record GetGymsQuery(string? Cursor, int? PageSize, int? OwnerId);

