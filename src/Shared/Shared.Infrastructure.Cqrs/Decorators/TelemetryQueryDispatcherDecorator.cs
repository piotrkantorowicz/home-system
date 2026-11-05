namespace Shared.Infrastructure.Cqrs.Decorators;

using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs.Telemetry;

internal sealed class TelemetryQueryDispatcherDecorator : IQueryDispatcher
{
    private readonly IQueryDispatcher _inner;
    private readonly CqrsTelemetry _telemetry;

    public TelemetryQueryDispatcherDecorator(IQueryDispatcher inner, CqrsTelemetry telemetry)
        => (_inner, _telemetry) = (inner, telemetry);

    public Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>
        => _telemetry.TrackAsync(
            CqrsTelemetry.Query,
            typeof(TQuery).Name,
            () => _inner.SendAsync<TQuery, TResult>(query, ct));
}
