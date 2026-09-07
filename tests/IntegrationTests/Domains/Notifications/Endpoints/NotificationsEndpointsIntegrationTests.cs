using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Domains.Notifications.Endpoints;

using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class NotificationsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private TestEmailNotificationSender _sender = null!;

    public async Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        _sender = _factory.Services.GetRequiredService<TestEmailNotificationSender>();
        _sender.Clear();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await SeedAuthorizedUserTokenAsync(asAdmin: true));
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task SendHtmlEndpoint_ShouldDispatchEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/notifications/emails/send-html", new
        {
            to = "delivered@test.com",
            subject = "HTML message",
            html = "<strong>Hello</strong>"
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var sentMessage = Assert.Single(_sender.Snapshot());
        Assert.Equal("delivered@test.com", sentMessage.To);
        Assert.Equal("HTML message", sentMessage.Subject);
        Assert.Equal("<strong>Hello</strong>", sentMessage.Html);
        Assert.Null(sentMessage.TemplateId);
    }

    [Fact]
    public async Task SendTemplateEndpoint_ShouldDispatchTemplateEmail()
    {
        var templateId = Guid.NewGuid().ToString();
        var response = await _client.PostAsJsonAsync("/api/notifications/emails/send-template", new
        {
            to = "template@test.com",
            subject = "Template message",
            templateId,
            variables = new
            {
                firstName = "Alan",
                weeklySessions = 4
            }
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var sentMessage = Assert.Single(_sender.Snapshot());
        Assert.Equal("template@test.com", sentMessage.To);
        Assert.Equal("Template message", sentMessage.Subject);
        Assert.Equal(templateId, sentMessage.TemplateId);
        Assert.Equal("Alan", sentMessage.Variables["firstName"]);
        Assert.Equal(4L, sentMessage.Variables["weeklySessions"]);
    }

    [Fact]
    public async Task SendHtmlEndpoint_NonAdminUser_ShouldReturnForbidden()
    {
        _sender.Clear();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await SeedAuthorizedUserTokenAsync(asAdmin: false));

        var response = await _client.PostAsJsonAsync("/api/notifications/emails/send-html", new
        {
            to = "delivered@test.com",
            subject = "HTML message",
            html = "<strong>Hello</strong>"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(_sender.Snapshot());
    }

    // native-authorization-model: sending emails requires PlatformRoleType.Admin.
    private async Task<string> SeedAuthorizedUserTokenAsync(bool asAdmin)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return TestFirebaseService.CreateToken(user.FirebaseUid, user.Email);
    }
}

