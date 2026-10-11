namespace Investments.IntegrationTests.Infrastructure;

using global::Investments.Application.Fx;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Boots the real host with the Investments and Household databases pointed at Testcontainers
/// Postgres and JWT auth replaced by <see cref="TestAuthHandler"/>. Other modules' databases are never touched.
/// </summary>
public sealed class InvestmentsApiFactory(InvestmentsDatabaseFixture databases) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Investments", databases.ConnectionString);
        builder.UseSetting("ConnectionStrings:Household", databases.HouseholdConnectionString);
        builder.UseSetting("Investments:NbpFx:Enabled", "false"); // tests run the worker on demand

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TestClock.Create());
            services.RemoveAll<INbpClient>();
            services.AddSingleton<INbpClient>(Nbp);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>The NBP stand-in every request in this host uses.</summary>
    public FakeNbpClient Nbp { get; } = new();

    /// <summary>Creates a client whose every request authenticates as the given subject via the test headers.</summary>
    /// <param name="sub">The Authentik subject to impersonate; unique per test to keep data isolated.</param>
    /// <param name="name">Display name claim.</param>
    public HttpClient CreateClientFor(string sub, string? name = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        client.DefaultRequestHeaders.Add("X-Test-Email", $"{Guid.NewGuid():N}@x.com");
        if (name is not null)
            client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }
}
