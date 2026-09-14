namespace DietPlanner.IntegrationTests.Infrastructure;

using DietPlanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Boots the real host in the <c>Testing</c> environment with the DietPlanner <c>DbContext</c> pointed
/// at a Testcontainers database and JWT auth replaced by <see cref="TestAuthHandler"/>. Background
/// reminder ticking is disabled so tests drive jobs directly.
/// </summary>
/// <param name="connectionString">Connection string of the test database.</param>
/// <param name="userId">Identity every request is authenticated as; <see cref="TestAuthHandler.TestUserId"/> when omitted.</param>
/// <param name="settings">Extra configuration overrides applied before the host builds.</param>
public sealed class DietPlannerWebApplicationFactory(
    string connectionString,
    string? userId = null,
    IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (settings is not null)
        {
            foreach (var (key, value) in settings)
                builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            // Replace the real DbContext with test container connection
            services.RemoveAll<DbContextOptions<DietPlannerDbContext>>();
            services.AddDbContext<DietPlannerDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Optionally override the test user identity (e.g. for isolation tests)
            if (userId is not null)
                services.AddSingleton(new TestUserIdOverride(userId));

            // Replace JWT auth with test stub
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });
        });
    }
}
