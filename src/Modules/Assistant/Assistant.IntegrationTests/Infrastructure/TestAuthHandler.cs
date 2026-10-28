namespace Assistant.IntegrationTests.Infrastructure;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Test authentication: the subject comes from the <c>X-Test-Sub</c> header, so one host serves many
/// users. A request without it is anonymous.
/// </summary>
/// <param name="options">Scheme options monitor supplied by ASP.NET.</param>
/// <param name="logger">Logger factory supplied by ASP.NET.</param>
/// <param name="encoder">URL encoder supplied by ASP.NET.</param>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Name the scheme is registered under.</summary>
    public const string SchemeName = "Test";

    /// <summary>Header carrying the subject.</summary>
    public const string SubjectHeader = "X-Test-Sub";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sub = Request.Headers[SubjectHeader].ToString();
        if (string.IsNullOrEmpty(sub))
            return Task.FromResult(AuthenticateResult.NoResult());

        Claim[] claims = [new(ClaimTypes.NameIdentifier, sub), new(ClaimTypes.Email, $"{sub}@example.com"), new("name", sub)];
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
