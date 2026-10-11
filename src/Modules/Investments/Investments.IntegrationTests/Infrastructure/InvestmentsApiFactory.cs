namespace Investments.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boots the real host with the Investments database pointed at Testcontainers Postgres and JWT
/// auth replaced by <see cref="TestAuthHandler"/>. Other modules' databases are never touched.
/// </summary>
public sealed class InvestmentsApiFactory(InvestmentsDatabaseFixture database) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Investments", database.ConnectionString);

        builder.ConfigureServices(services =>
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }

    /// <summary>Creates a client whose every request authenticates as the given subject via the test headers.</summary>
    /// <param name="sub">The Authentik subject to impersonate.</param>
    public HttpClient CreateClientFor(string sub)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        return client;
    }
}
