namespace Shared.Testing.Mcp;

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Web;

/// <summary>
/// Evidence for #710: <c>ApplicationExceptionHandler</c> answers any cancellation with a silent 499, so log every 499 the host
/// sends: the request, whether it was aborted, whether the host was stopping and how long it took.
/// </summary>
/// <remarks>
/// A passing test's console output never reaches the CI log, and the connect retry makes the flaky test pass, so lines also go
/// to <c>mcp-499-probe.log</c> next to the test assembly, which the backend CI uploads with the test results.
/// </remarks>
public sealed class Mcp499Probe : IStartupFilter
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "mcp-499-probe.log");

    /// <summary>Writes one probe line to the console and to the probe log file.</summary>
    public static void Log(string message)
    {
        Console.WriteLine($"[mcp-499-probe] {message}");
        try
        {
            File.AppendAllText(LogPath, $"{TimeProvider.System.GetUtcNow():O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Best effort: the probe must never fail a test.
        }
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            app.Use(async (context, nextMiddleware) =>
            {
                var started = Stopwatch.GetTimestamp();
                try
                {
                    await nextMiddleware(context);
                }
                finally
                {
                    if (context.Response.StatusCode == ApplicationExceptionHandler.ClientClosedRequestStatusCode)
                        Log($"499 {context.Request.Method} {context.Request.Path} aborted={context.RequestAborted.IsCancellationRequested} "
                            + $"stopping={lifetime.ApplicationStopping.IsCancellationRequested} elapsedMs={Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0}");
                }
            });
            next(app);
        };
}
