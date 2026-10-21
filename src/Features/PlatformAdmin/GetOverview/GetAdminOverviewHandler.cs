using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.Safety.Shared;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.PlatformAdmin.GetOverview;

public record GetAdminOverviewQuery;

public record AdminOverviewResponse(
    int UsersTotal,
    int UsersActive,
    int CredentialsUnderReview,
    int OpenSafetyReports);

public class GetAdminOverviewHandler(
    IUserRepository users,
    IProfessionalCredentialRepository credentials,
    ISafetyReportCounts reports)
{
    public async Task<Result<AdminOverviewResponse>> HandleAsync(
        GetAdminOverviewQuery query, CancellationToken cancellationToken)
    {
        var usersTotal = await users.CountAsync(null, cancellationToken);
        var usersActive = await users.CountAsync(true, cancellationToken);
        var underReview = await credentials.CountByStatusAsync(CredentialStatus.UnderReview, cancellationToken);
        var openReports = await reports.CountByStatusAsync(ReportStatuses.Open, cancellationToken);

        return Result<AdminOverviewResponse>.Success(
            new AdminOverviewResponse(usersTotal, usersActive, underReview, openReports));
    }
}
