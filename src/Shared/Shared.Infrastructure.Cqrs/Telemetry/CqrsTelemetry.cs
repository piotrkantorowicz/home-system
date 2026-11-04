namespace Shared.Infrastructure.Cqrs.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>
/// One span and one duration sample per dispatched command or query. Tags carry the request type
/// name and the outcome only — never request content (docs/rules/backend-observability.md).
/// </summary>
internal sealed class CqrsTelemetry
{
    public const string SourceName = "HomeSystem.Cqrs";
    public const string MeterName = "HomeSystem.Cqrs";
    public const string Command = "command";
    public const string Query = "query";

    private static readonly ActivitySource Source = new(SourceName);

    private readonly Histogram<double> _duration;

    public CqrsTelemetry(IMeterFactory meters)
        => _duration = meters.Create(MeterName).CreateHistogram<double>("homesystem.cqrs.duration", unit: "s");

    public async Task<T> TrackAsync<T>(string kind, string name, Func<Task<T>> dispatch)
    {
        using var activity = Source.StartActivity(name);
        activity?.SetTag("cqrs.kind", kind);
        activity?.SetTag("cqrs.name", name);

        var start = Stopwatch.GetTimestamp();
        var outcome = "success";
        try
        {
            return await dispatch();
        }
        catch (Exception ex)
        {
            outcome = ex switch
            {
                CommandValidationException => "validation_error",
                NotFoundException => "not_found",
                _ => "error",
            };

            // Validation and not-found are caller mistakes, not faults: only "error" marks the span failed.
            if (outcome == "error")
                activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);

            throw;
        }
        finally
        {
            activity?.SetTag("cqrs.outcome", outcome);
            _duration.Record(
                Stopwatch.GetElapsedTime(start).TotalSeconds,
                new("cqrs.kind", kind),
                new("cqrs.name", name),
                new("cqrs.outcome", outcome));
        }
    }

    public async Task TrackAsync(string kind, string name, Func<Task> dispatch)
        => await TrackAsync(kind, name, async () =>
        {
            await dispatch();
            return true;
        });
}
