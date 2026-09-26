namespace Notifications.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boots the real host with the Notifications database pointed at the suite's Testcontainers
/// Postgres and JWT auth replaced by <see cref="TestAuthHandler"/>.
/// </summary>
/// <param name="connectionString">The Notifications database the host should use.</param>
public sealed class NotificationsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Notifications", connectionString);

        builder.ConfigureServices(services =>
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { }));
    }

    /// <summary>Creates a client authenticated as a fresh subject with the given roles.</summary>
    /// <param name="roles">Values for the <c>roles</c> claim.</param>
    public HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", $"auth|{Guid.NewGuid():N}");
        if (roles.Length > 0) client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(',', roles));
        return client;
    }
}
