namespace Household.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

/// <summary>
/// Boots the real host with the Household database pointed at a Testcontainers Postgres and
/// JWT auth replaced by <see cref="TestAuthHandler"/>. Other modules' databases are never
/// touched by these tests.
/// </summary>
public sealed class HouseholdApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <summary>The clock the host runs on; advance it to move "now" for every request.</summary>
    public FakeTimeProvider Clock { get; } = TestClock.Create();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Household", connectionString);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Creates a client whose every request authenticates as the given identity via the test headers.</summary>
    /// <param name="sub">The Authentik subject to impersonate; unique per test to keep data isolated.</param>
    /// <param name="email">Optional email claim, used by invitation and account-link scenarios.</param>
    /// <param name="name">Optional display name claim.</param>
    public HttpClient CreateClientFor(string sub, string? email = null, string? name = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        if (email is not null) client.DefaultRequestHeaders.Add("X-Test-Email", email);
        if (name is not null) client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }
}
