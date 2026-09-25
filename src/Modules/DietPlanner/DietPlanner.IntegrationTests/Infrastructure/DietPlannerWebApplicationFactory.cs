namespace DietPlanner.IntegrationTests.Infrastructure;

using DietPlanner.Infrastructure.Persistence;
using Household.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

/// <summary>
/// Boots the real host in the <c>Testing</c> environment with the DietPlanner <c>DbContext</c> pointed
/// at a Testcontainers database and JWT auth replaced by <see cref="TestAuthHandler"/>. Background
/// reminder ticking is disabled so tests drive jobs directly.
/// </summary>
/// <param name="connectionString">Connection string of the test database.</param>
/// <param name="userId">Identity every request is authenticated as; <see cref="TestAuthHandler.TestUserId"/> when omitted.</param>
/// <param name="settings">Extra configuration overrides applied before the host builds.</param>
/// <param name="clock">Clock the host reads time from; a <see cref="FakeTimeProvider"/> pinned at <see cref="TestClock.Now"/> when omitted.</param>
public sealed class DietPlannerWebApplicationFactory(
    string connectionString,
    string? userId = null,
    IReadOnlyDictionary<string, string?>? settings = null,
    FakeTimeProvider? clock = null)
    : WebApplicationFactory<Program>
{
    /// <summary>The clock the host runs on; advance it to move "now" for every request.</summary>
    public FakeTimeProvider Clock { get; } = clock ?? TestClock.Create();

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

            // Pin the wall clock so timestamps and "today" are deterministic
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            if (settings is null || !settings.ContainsKey("ConnectionStrings:Household"))
            {
                services.AddSingleton<TestHouseholdQueryService>();
                services.RemoveAll<IHouseholdQueryService>();
                services.AddSingleton<IHouseholdQueryService>(sp => sp.GetRequiredService<TestHouseholdQueryService>());
            }

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
