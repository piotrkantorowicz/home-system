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
        var scope = Begin(kind, name);
        try
        {
            var result = await dispatch();
            scope.End(null);
            return result;
        }
        catch (Exception ex)
        {
            scope.End(ex);
            throw;
        }
    }

    public async Task TrackAsync(string kind, string name, Func<Task> dispatch)
    {
        var scope = Begin(kind, name);
        try
        {
            await dispatch();
            scope.End(null);
        }
        catch (Exception ex)
        {
            scope.End(ex);
            throw;
        }
    }

    // The span starts before the handler runs, so the handler's own spans are its children.
    private Scope Begin(string kind, string name)
    {
        var activity = Source.StartActivity(name);
        activity?.SetTag("cqrs.kind", kind);
        activity?.SetTag("cqrs.name", name);
        return new Scope(_duration, kind, name, activity, Stopwatch.GetTimestamp());
    }

    private readonly struct Scope(Histogram<double> duration, string kind, string name, Activity? activity, long start)
    {
        /// <summary>Records the outcome and ends the span; called exactly once, with the failure or null.</summary>
        public void End(Exception? failure)
        {
            var outcome = failure switch
            {
                null => "success",
                CommandValidationException => "validation_error",
                NotFoundException => "not_found",
                ForbiddenException => "forbidden",
                ConflictException => "conflict",
                DomainException => "rejected",
                OperationCanceledException => "cancelled",
                _ => "error",
            };

            // Rejections the API answers with 4xx, and client aborts, are not faults:
            // only "error" marks the span failed.
            if (outcome == "error")
                activity?.SetStatus(ActivityStatusCode.Error, failure!.GetType().Name);

            activity?.SetTag("cqrs.outcome", outcome);
            duration.Record(
                Stopwatch.GetElapsedTime(start).TotalSeconds,
                new("cqrs.kind", kind),
                new("cqrs.name", name),
                new("cqrs.outcome", outcome));
            activity?.Dispose();
        }
    }
}
