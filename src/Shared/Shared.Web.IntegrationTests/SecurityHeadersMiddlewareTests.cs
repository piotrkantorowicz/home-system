namespace Shared.Web.IntegrationTests;

using System.Net;
using Microsoft.AspNetCore.Http;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Web;
using Shared.Web.IntegrationTests.Infrastructure;

/// <summary>
/// <see cref="SecurityHeadersMiddleware"/> replaces the inline <c>app.Use</c> the host had; both successful and
/// error responses must still carry the three hardening headers.
/// </summary>
public sealed class SecurityHeadersMiddlewareTests
{
    /// <summary>A normal response carries every hardening header.</summary>
    [Fact]
    public async Task Invoke_OnSuccessfulResponse_AddsSecurityHeaders()
    {
        await using var host = await ErrorPipelineHost.StartAsync(ctx => ctx.Response.WriteAsync("ok"));
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertSecurityHeaders(response);
    }

    /// <summary>The headers are stamped before the pipeline runs, so a mapped error response keeps them.</summary>
    [Fact]
    public async Task Invoke_OnErrorResponse_KeepsSecurityHeaders()
    {
        await using var host = await ErrorPipelineHost.StartThrowingAsync(new NotFoundException("missing"));
        using var client = host.CreateClient();

        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertSecurityHeaders(response);
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        response.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        response.Headers.GetValues("X-Frame-Options").ShouldBe(["DENY"]);
        response.Headers.GetValues("X-XSS-Protection").ShouldBe(["1; mode=block"]);
    }
}
