namespace DietPlanner.IntegrationTests.Infrastructure;

using DietPlanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class DietPlannerWebApplicationFactory(string connectionString, string? userId = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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
