using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.UserManagement;
using ShapeUp.Features.Authorization.UserManagement.MobileExperience;

namespace IntegrationTests.Domains.Authorization.MobileExperience;

public class MobileExperienceEndpointTests
{
    private static TestServer Server(bool active = true)
    {
        var stamp = DateTime.UtcNow;
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(
            new User { Id = 7, FirebaseUid = "u", Email = "u@example.test", IsActive = active, FirstMobileSeenAtUtc = stamp });
        return new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(UserManagementController).Assembly);
            services.AddSingleton(repo.Object);
            services.AddScoped<RegisterMobileExperienceHandler>();
            services.AddScoped<IValidator<RegisterMobileExperienceCommand>, RegisterMobileExperienceValidator>();
            services.AddMobileExperienceRateLimit();
        }).Configure(app =>
        {
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.ContainsKey("Test-User"))
                    context.Items["User"] = new UserContext(7, "u", "u@example.test", null);
                await next();
            });
            app.UseRateLimiter();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));
    }

    [Theory]
    [InlineData("ios", true, true, 200)]
    [InlineData("android", true, true, 200)]
    [InlineData("web", true, true, 400)]
    [InlineData("ios", false, true, 401)]
    [InlineData("ios", true, false, 403)]
    public async Task ContractStatuses(string platform, bool authenticated, bool active, int expected)
    {
        using var server = Server(active);
        using var client = server.CreateClient();
        if (authenticated) client.DefaultRequestHeaders.Add("Test-User", "7");
        var response = await client.PostAsJsonAsync("/api/users/me/mobile-experience", new { platform });
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task TwentyRequestsThenRateLimited()
    {
        using var server = Server();
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Test-User", "7");
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/users/me/mobile-experience", new { platform = "ios" })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/users/me/mobile-experience", new { platform = "android" })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentPlatformsClaimOnlyOneFirstVisit()
    {
        await using var sql = new MsSqlBuilder().Build();
        await sql.StartAsync();
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>().UseSqlServer(sql.GetConnectionString()).Options;
        await using (var setup = new AuthorizationDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Users.Add(new User { FirebaseUid = "concurrent", Email = "concurrent@example.test" });
            await setup.SaveChangesAsync();
        }
        var attempts = Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var db = new AuthorizationDbContext(options);
            return await new UserRepository(db).TryRegisterMobileExperienceAsync(1, DateTime.UtcNow, CancellationToken.None);
        });
        Assert.Equal(1, (await Task.WhenAll(attempts)).Count(first => first));
    }
}
