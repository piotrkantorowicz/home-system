namespace Shared.Infrastructure.Messaging.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Messaging.Transport;

public sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxWorkerOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> options,
        ILogger<OutboxWorker> logger)
        => (_scopeFactory, _options, _logger) = (scopeFactory, options.Value, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox worker tick failed");
            }

            try
            {
                await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetService<IOutboxStore>();
        if (store is null) return;
        var transport = scope.ServiceProvider.GetService<IIntegrationEventTransport>();
        if (transport is null) return;

        var pending = await store.GetUnprocessedAsync(_options.BatchSize, ct).ConfigureAwait(false);
        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                await transport.DispatchAsync(message, ct).ConfigureAwait(false);
                await store.MarkProcessedAsync(message.Id, DateTime.UtcNow, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex,
                    "Failed to dispatch outbox message {MessageId} ({EventType})",
                    message.Id, message.EventType);
                await store.RecordFailureAsync(message.Id, ex.Message, ct).ConfigureAwait(false);
            }
        }
    }
}
