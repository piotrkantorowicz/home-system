namespace Shared.Web.IntegrationTests.Infrastructure;

using System.Diagnostics;
using HomeSystem.REST.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs.Extensions;

/// <summary>
/// A minimal in-memory host with <c>HomeSystem.REST</c>'s <see cref="ObservabilityExtensions.AddHomeSystemObservability"/>
/// and capturing processors instead of an exporter. <c>/items/{id}</c> logs and runs one parameterised query;
/// <c>/commands/ping</c> dispatches <see cref="PingCommand"/>; <c>/health/live</c> does nothing.
/// </summary>
internal sealed class ObservabilityHost : IAsyncDisposable
{
    /// <summary>The value bound to the query's parameter; it must never reach a span.</summary>
    public const string ParameterValue = "parameter-value-must-not-leak";

    /// <summary>The logger category <c>/items/{id}</c> writes to.</summary>
    public const string LogCategory = "ObservabilityHost.Items";

    private static readonly TimeSpan SpanWait = TimeSpan.FromSeconds(5);

    private static readonly Action<ILogger, int, Exception?> ReadingItem =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, nameof(ReadingItem)), "Reading item {ItemId}");

    private readonly WebApplication _app;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CapturingProcessor<Activity, Activity> _spans;
    private readonly CapturingProcessor<LogRecord, (string? Category, ActivityTraceId TraceId)> _logs;

    private ObservabilityHost(
        WebApplication app,
        NpgsqlDataSource dataSource,
        CapturingProcessor<Activity, Activity> spans,
        CapturingProcessor<LogRecord, (string? Category, ActivityTraceId TraceId)> logs)
        => (_app, _dataSource, _spans, _logs) = (app, dataSource, spans, logs);

    /// <summary>Every finished span, from any host in the process — filter by what the test sent.</summary>
    public IReadOnlyList<Activity> Spans => _spans.Items;

    /// <summary>Category and trace ID of every log record written.</summary>
    public IReadOnlyList<(string? Category, ActivityTraceId TraceId)> Logs => _logs.Items;

    /// <summary>Starts the host against <paramref name="connectionString"/>, with export switched off.</summary>
    /// <param name="connectionString">The database <c>/items/{id}</c> queries.</param>
    /// <param name="ct">Cancels the host start.</param>
    public static async Task<ObservabilityHost> StartAsync(string connectionString, CancellationToken ct)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[ObservabilityExtensions.OtlpEndpointVariable] = string.Empty;
        builder.AddHomeSystemObservability();

        var spans = new CapturingProcessor<Activity, Activity>(span => span);
        var logs = new CapturingProcessor<LogRecord, (string? Category, ActivityTraceId TraceId)>(
            record => (record.CategoryName, record.TraceId));
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(spans));
        builder.Services.ConfigureOpenTelemetryLoggerProvider(logging => logging.AddProcessor(logs));

        builder.Services.AddScoped<ICommandHandler<PingCommand>, PingCommandHandler>();
        builder.Services.AddCqrsDispatchers();

        var dataSource = NpgsqlDataSource.Create(connectionString);
        var app = builder.Build();
        app.MapGet("/items/{id}", async (int id, ILoggerFactory loggers, CancellationToken ct) =>
        {
            ReadingItem(loggers.CreateLogger(LogCategory), id, null);
            await using var command = dataSource.CreateCommand("SELECT $1::text");
            command.Parameters.Add(new NpgsqlParameter { Value = ParameterValue });
            await command.ExecuteScalarAsync(ct);
            return TypedResults.Ok();
        });
        app.MapPost("/commands/ping", async (ICommandDispatcher dispatcher, CancellationToken ct) =>
        {
            await dispatcher.SendAsync(new PingCommand(), ct);
            return TypedResults.NoContent();
        });
        app.MapGet("/health/live", () => TypedResults.Ok());

        await app.StartAsync(ct);
        return new ObservabilityHost(app, dataSource, spans, logs);
    }

    /// <summary>A client talking to the in-memory server.</summary>
    public HttpClient CreateClient() => _app.GetTestClient();

    /// <summary>
    /// The first finished span matching <paramref name="match"/>. The server span ends after the response is
    /// handed back, so it may land a moment after the request returns.
    /// </summary>
    /// <param name="match">Which span to wait for.</param>
    public Activity WaitForSpan(Func<Activity, bool> match)
    {
        SpinWait.SpinUntil(() => Spans.Any(match), SpanWait).ShouldBeTrue();
        return Spans.First(match);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _dataSource.DisposeAsync();
    }
}
