using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.Credentials.Shared.Entities;
using ShapeUp.Features.PlatformAdmin.GetOverview;
using ShapeUp.Features.Safety.Shared;

namespace UnitTests.Domains.PlatformAdmin;

public class GetAdminOverviewHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsDatabaseCounts()
    {
        var users = new Mock<IUserRepository>();
        var credentials = new Mock<IProfessionalCredentialRepository>();
        var reports = new Mock<ISafetyReportCounts>();
        users.Setup(x => x.CountAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(12);
        users.Setup(x => x.CountAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(10);
        credentials.Setup(x => x.CountByStatusAsync(CredentialStatus.UnderReview, It.IsAny<CancellationToken>())).ReturnsAsync(3);
        reports.Setup(x => x.CountByStatusAsync(ReportStatuses.Open, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var handler = new GetAdminOverviewHandler(users.Object, credentials.Object, reports.Object);
        var result = await handler.HandleAsync(new GetAdminOverviewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value!.UsersTotal);
        Assert.Equal(10, result.Value.UsersActive);
        Assert.Equal(3, result.Value.CredentialsUnderReview);
        Assert.Equal(2, result.Value.OpenSafetyReports);
    }
}
