namespace Shared.Web.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shared.Infrastructure.Web;

/// <summary>
/// A minimal in-memory host wired exactly like <c>HomeSystem.REST</c>'s error pipeline —
/// <c>AddProblemDetails</c> + <see cref="ApplicationExceptionHandler"/> + <c>UseExceptionHandler</c> with the
/// diagnostics callback + <see cref="SecurityHeadersMiddleware"/> — in front of one terminal delegate the test
/// supplies. Every log line the framework writes is captured in <see cref="Logs"/>.
/// </summary>
internal sealed class ErrorPipelineHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ErrorPipelineHost(WebApplication app)
        => _app = app;

    /// <summary>Everything logged while the host was running, in order.</summary>
    public FakeLogCollector Logs => _app.Services.GetRequiredService<FakeLogCollector>();

    /// <summary>Starts a host whose only endpoint runs <paramref name="terminal"/>.</summary>
    /// <param name="terminal">The request delegate every request ends in; typically throws.</param>
    public static async Task<ErrorPipelineHost> StartAsync(RequestDelegate terminal)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddFakeLogging();
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();

        var app = builder.Build();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            SuppressDiagnosticsCallback = ApplicationExceptionHandler.ShouldSuppressDiagnostics,
        });
        app.Run(terminal);

        await app.StartAsync();
        return new ErrorPipelineHost(app);
    }

    /// <summary>Starts a host whose only endpoint throws <paramref name="exception"/>.</summary>
    /// <param name="exception">The exception the pipeline must map.</param>
    public static Task<ErrorPipelineHost> StartThrowingAsync(Exception exception)
        => StartAsync(_ => Task.FromException(exception));

    /// <summary>A client talking to the in-memory server.</summary>
    public HttpClient CreateClient() => _app.GetTestClient();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
