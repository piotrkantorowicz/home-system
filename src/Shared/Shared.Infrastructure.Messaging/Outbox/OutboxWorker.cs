namespace Shared.Infrastructure.Messaging.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Messaging.Transport;

public sealed partial class OutboxWorker<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private static readonly string DbContextName = typeof(TDbContext).Name;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxWorkerOptions _options;
    private readonly ILogger<OutboxWorker<TDbContext>> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> options,
        ILogger<OutboxWorker<TDbContext>> logger)
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
                LogTickFailed(ex, DbContextName);
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
        var sp = scope.ServiceProvider;

        // Resolve this module's store via the keyed registration made by AddOutbox<TDbContext>().
        var store = sp.GetKeyedService<IOutboxStore>(typeof(TDbContext));
        if (store is null)
        {
            LogNoStore(DbContextName);
            return;
        }

        var transport = sp.GetService<IIntegrationEventTransport>();
        if (transport is null)
        {
            LogNoTransport(DbContextName);
            return;
        }

        IReadOnlyList<OutboxMessage> pending =
            await store.GetUnprocessedAsync(_options.BatchSize, ct).ConfigureAwait(false);
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
                LogDispatchFailed(ex, message.Id, message.EventType, DbContextName);
                await store.RecordFailureAsync(message.Id, ex.Message, ct).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox worker [{DbContext}] tick failed")]
    private partial void LogTickFailed(Exception exception, string dbContext);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox worker [{DbContext}] no-op: no outbox store registered")]
    private partial void LogNoStore(string dbContext);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox worker [{DbContext}] no-op: no transport registered")]
    private partial void LogNoTransport(string dbContext);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to dispatch outbox message {MessageId} ({EventType}) [{DbContext}]")]
    private partial void LogDispatchFailed(Exception exception, Guid messageId, string eventType, string dbContext);
}
