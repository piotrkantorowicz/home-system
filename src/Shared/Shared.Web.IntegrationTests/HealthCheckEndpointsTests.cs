namespace Shared.Web.IntegrationTests;

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The Kubernetes probes on the real host: live never touches a dependency, ready fails while a module database
/// is unreachable. The healthy path (real Postgres, unreachable Authentik → Degraded, still 200) is covered by
/// scripts/smoke-api-image.sh.
/// </summary>
public sealed class HealthCheckEndpointsTests
{
    private static readonly string[] Modules = ["DietPlanner", "Notifications", "Household", "Budget", "Investments"];

    /// <summary>No module database answers: live stays 200, ready is 503.</summary>
    [Fact]
    public async Task Probes_WhenTheDatabasesAreUnreachable_LiveIsHealthyAndReadyIsUnhealthy()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var root = new WebApplicationFactory<Program>();
        var factory = root.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Authentication:Authentik:Authority", "https://auth.example.test");
            foreach (var module in Modules)
            {
                // Port 1 refuses at once, so the check fails without waiting for a connect timeout.
                builder.UseSetting($"ConnectionStrings:{module}", "Host=127.0.0.1;Port=1;Database=none;Username=none;Timeout=2");
            }

            builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
        });
        using var client = factory.CreateClient();

        using var live = await client.GetAsync("/health/live", ct);
        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await live.Content.ReadAsStringAsync(ct)).ShouldBe("Healthy");

        using var ready = await client.GetAsync("/health/ready", ct);
        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await ready.Content.ReadAsStringAsync(ct)).ShouldBe("Unhealthy");
    }
}
