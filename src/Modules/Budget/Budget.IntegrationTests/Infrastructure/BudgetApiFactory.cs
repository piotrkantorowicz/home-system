namespace Budget.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Boots the real host with the Budget and Household databases pointed at Testcontainers Postgres
/// and JWT auth replaced by <see cref="TestAuthHandler"/>. Other modules' databases are never touched.
/// </summary>
public sealed class BudgetApiFactory(BudgetDatabaseFixture databases) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Budget", databases.BudgetConnectionString);
        builder.UseSetting("ConnectionStrings:Household", databases.HouseholdConnectionString);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TestClock.Create());

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Creates a client whose every request authenticates as the given subject via the test headers.</summary>
    /// <param name="sub">The Authentik subject to impersonate; unique per test to keep data isolated.</param>
    /// <param name="name">Display name claim.</param>
    /// <param name="email">Email claim; a unique address when omitted.</param>
    public HttpClient CreateClientFor(string sub, string name, string? email = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        client.DefaultRequestHeaders.Add("X-Test-Email", email ?? $"{Guid.NewGuid():N}@x.com");
        client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }
}
