using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.ExerciseRequests.Shared;

/// <summary>
/// Builds the read model: resolves equipment names and people names in bulk. The reviewer and the admin note are only
/// filled for the admin view, never for the requester.
/// </summary>
public class ExerciseRequestResponseBuilder(IEquipmentRepository equipmentRepository, IUserRepository userRepository)
{
    public async Task<ExerciseRequestResponse[]> BuildAsync(
        IReadOnlyList<ExerciseRequest> requests, bool includeReviewer, CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
            return [];

        var equipmentIds = requests.SelectMany(r => ParseIds(r.EquipmentIds)).Distinct().ToArray();
        var equipmentNames = new Dictionary<int, string>();
        foreach (var id in equipmentIds)
        {
            var equipment = await equipmentRepository.GetByIdAsync(id, cancellationToken);
            if (equipment is not null)
                equipmentNames[id] = equipment.Name;
        }

        var userIds = requests.Select(r => r.RequestedByUserId)
            .Concat(includeReviewer ? requests.Where(r => r.ReviewedByUserId.HasValue).Select(r => r.ReviewedByUserId!.Value) : [])
            .Where(id => id > 0).Distinct().ToArray();
        var names = (await userRepository.GetByIdsAsync(userIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName ?? u.Email);

        return requests.Select(r =>
        {
            var ids = ParseIds(r.EquipmentIds);
            return new ExerciseRequestResponse(
                r.Id, r.Status, r.Name, r.Kind, r.ExerciseType,
                r.MuscleGroups.Split(',', StringSplitOptions.RemoveEmptyEntries),
                ids,
                ids.Where(equipmentNames.ContainsKey).Select(i => equipmentNames[i]).ToArray(),
                r.EquipmentOther, r.Notes, r.ReferenceUrl,
                r.RequestedByUserId, names.GetValueOrDefault(r.RequestedByUserId), r.RequestedByRole, r.CreatedAtUtc,
                includeReviewer ? r.ReviewedByUserId : null,
                includeReviewer && r.ReviewedByUserId.HasValue ? names.GetValueOrDefault(r.ReviewedByUserId.Value) : null,
                r.ReviewedAtUtc, r.ApprovedExerciseId, r.ApprovedExerciseName, r.RejectionReason,
                includeReviewer ? r.AdminNote : null);
        }).ToArray();
    }

    public static int[] ParseIds(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToArray();
}
