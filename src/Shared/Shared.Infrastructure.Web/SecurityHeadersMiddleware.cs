namespace Shared.Infrastructure.Web;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Stamps the baseline browser hardening headers on every response: <c>X-Content-Type-Options: nosniff</c>,
/// <c>X-Frame-Options: DENY</c> and <c>X-XSS-Protection: 1; mode=block</c>.
/// </summary>
/// <remarks>
/// The headers are written in <see cref="HttpResponse.OnStarting(Func{Task})"/> rather than up front: the
/// exception handler middleware clears the response before it re-runs the pipeline for an error, and a callback
/// survives that clear while a header set eagerly does not. That also makes the registration order irrelevant.
/// </remarks>
/// <param name="next">The rest of the pipeline.</param>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>Schedules the headers for the moment the response starts, then continues the pipeline.</summary>
    /// <param name="context">The current request.</param>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "1; mode=block";
            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
