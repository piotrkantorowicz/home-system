namespace Household.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boots the real host with the Household database pointed at a Testcontainers Postgres and
/// JWT auth replaced by <see cref="TestAuthHandler"/>. Other modules' databases are never
/// touched by these tests.
/// </summary>
public sealed class HouseholdApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Household", connectionString);

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateClientFor(string sub, string? email = null, string? name = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        if (email is not null) client.DefaultRequestHeaders.Add("X-Test-Email", email);
        if (name is not null) client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }
}
