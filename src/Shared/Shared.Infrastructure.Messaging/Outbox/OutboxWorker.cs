namespace Shared.Infrastructure.Messaging.Outbox;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Messaging.Transport;

/// <summary>
/// Hosted service that drains one publishing module's outbox: every <see cref="OutboxWorkerOptions.PollIntervalMs"/>
/// it reads a batch of pending rows from the module's keyed <see cref="IOutboxStore"/>, hands each to
/// the <see cref="Transport.IIntegrationEventTransport"/> and marks it processed or records the
/// failure. Registered per module by <c>AddOutbox&lt;TDbContext&gt;()</c>; a tick that throws is
/// logged and the loop continues.
/// </summary>
/// <typeparam name="TDbContext">The publishing module's <c>DbContext</c>, which keys its outbox store.</typeparam>
/// <param name="scopeFactory">Opens a DI scope per tick so the store uses a fresh <c>DbContext</c>.</param>
/// <param name="options">Batch size and poll interval.</param>
/// <param name="logger">Receives tick and dispatch failures.</param>
/// <param name="clock">Supplies the <c>processed_at</c> timestamp.</param>
public sealed partial class OutboxWorker<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxWorkerOptions> options,
    ILogger<OutboxWorker<TDbContext>> logger,
    TimeProvider clock) : BackgroundService
    where TDbContext : DbContext
{
    private static readonly string DbContextName = typeof(TDbContext).Name;

    private readonly OutboxWorkerOptions _options = options.Value;

    /// <inheritdoc />
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

    /// <summary>
    /// Performs a single tick: dispatches up to <see cref="OutboxWorkerOptions.BatchSize"/> pending
    /// messages. Public so tests drive the worker deterministically instead of waiting on the timer.
    /// A no-op until the module has registered a store and the host a transport.
    /// </summary>
    /// <param name="ct">Propagates cancellation to the store and transport.</param>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
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
            await store.GetUnprocessedAsync(_options.BatchSize, _options.MaxAttempts, ct).ConfigureAwait(false);
        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                await transport.DispatchAsync(message, ct).ConfigureAwait(false);
                await store.MarkProcessedAsync(message.Id, clock.GetUtcNow().UtcDateTime, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogDispatchFailed(ex, message.Id, message.EventType, DbContextName);
                await store.RecordFailureAsync(message.Id, ex.Message, ct).ConfigureAwait(false);
            }
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Outbox worker [{DbContext}] tick failed")]
    private partial void LogTickFailed(Exception exception, string dbContext);

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Outbox worker [{DbContext}] no-op: no outbox store registered")]
    private partial void LogNoStore(string dbContext);

    [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Outbox worker [{DbContext}] no-op: no transport registered")]
    private partial void LogNoTransport(string dbContext);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Failed to dispatch outbox message {MessageId} ({EventType}) [{DbContext}]")]
    private partial void LogDispatchFailed(Exception exception, Guid messageId, string eventType, string dbContext);
}
