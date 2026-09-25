namespace DietPlanner.IntegrationTests.Infrastructure;

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Allows per-factory user-ID override for isolation in tests.</summary>
public sealed record TestUserIdOverride(string UserId);

/// <summary>
/// Test authentication scheme: every request succeeds as a fixed user (<see cref="TestUserId"/>, or
/// the <see cref="TestUserIdOverride"/> registered by the factory) with <c>sub</c> and
/// <c>NameIdentifier</c> claims, so endpoints see a normal authenticated principal without Authentik.
/// </summary>
/// <param name="options">Scheme options monitor supplied by ASP.NET.</param>
/// <param name="logger">Logger factory supplied by ASP.NET.</param>
/// <param name="encoder">URL encoder supplied by ASP.NET.</param>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private static readonly ConcurrentDictionary<string, Guid> Persons = new();

    internal static Guid PersonIdFor(string subject) => Persons.GetOrAdd(subject, _ => Guid.CreateVersion7());
    internal static string? SubjectFor(Guid personId)
        => Persons.FirstOrDefault(p => p.Value == personId).Key;

    /// <summary>Auth subject used when no override is registered.</summary>
    public const string TestUserId = "test-user-001";
    /// <summary>Name the scheme is registered under.</summary>
    public const string SchemeName = "Test";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Context.RequestServices.GetService<TestUserIdOverride>()?.UserId ?? TestUserId;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("preferred_username", userId),
            new Claim("sub", userId),
        };
        if (Context.RequestServices.GetService<TestHouseholdQueryService>() is not null)
            claims.Add(new Claim("person_id", PersonIdFor(userId).ToString()));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
