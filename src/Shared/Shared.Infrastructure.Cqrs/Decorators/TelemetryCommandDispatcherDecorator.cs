namespace Shared.Infrastructure.Cqrs.Decorators;

using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Cqrs.Telemetry;

internal sealed class TelemetryCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly CqrsTelemetry _telemetry;

    public TelemetryCommandDispatcherDecorator(ICommandDispatcher inner, CqrsTelemetry telemetry)
        => (_inner, _telemetry) = (inner, telemetry);

    public Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
        => _telemetry.TrackAsync(CqrsTelemetry.Command, typeof(TCommand).Name, () => _inner.SendAsync(command, ct));

    public Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
        => _telemetry.TrackAsync(
            CqrsTelemetry.Command,
            typeof(TCommand).Name,
            () => _inner.SendAsync<TCommand, TResult>(command, ct));
}
