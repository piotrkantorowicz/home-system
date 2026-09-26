namespace Household.IntegrationTests.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Test authentication: reads the desired identity from request headers so a single host can
/// serve many "users". Headers: <c>X-Test-Sub</c> (required), <c>X-Test-Email</c>,
/// <c>X-Test-Name</c>, <c>X-Test-Roles</c> (comma-separated, emitted as <c>roles</c> claims). A request without <c>X-Test-Sub</c> is treated as anonymous.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Name the scheme is registered under.</summary>
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sub = Request.Headers["X-Test-Sub"].ToString();
        if (string.IsNullOrEmpty(sub))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, sub) };

        var email = Request.Headers["X-Test-Email"].ToString();
        if (!string.IsNullOrEmpty(email))
            claims.Add(new Claim(ClaimTypes.Email, email));

        var name = Request.Headers["X-Test-Name"].ToString();
        if (!string.IsNullOrEmpty(name))
            claims.Add(new Claim("name", name));

        foreach (var role in Request.Headers["X-Test-Roles"].ToString()
                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            claims.Add(new Claim("roles", role));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
