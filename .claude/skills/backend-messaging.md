# Backend — Messaging Infrastructure (Full Source)

> Complete source for the integration-event bus + outbox/inbox stack. v1 transport is
> in-process; the seam is broker-ready.
>
> **Project layout:**
> - `Shared.Abstractions.Messaging/` — `IIntegrationEvent`, `IIntegrationEventBus`, `IIntegrationEventHandler<T>`, `IInboxExecutor`
> - `Shared.Infrastructure.Messaging/` — `OutboxIntegrationEventBus`, `IOutboxStore`, `OutboxMessage`, `OutboxWorker(+Options)`, `IIntegrationEventTransport`, `InProcessIntegrationEventTransport`, `IIntegrationEventSerializer` / `IntegrationEventSerializer`, `MessagingExtensions` / `MessagingBuilder`
> - `Shared.Infrastructure.Messaging.Ef/` — `OutboxMessageEntity` (+ configuration), `EfOutboxStore<TDbContext>`, `InboxMessageEntity` (+ configuration), `EfInboxExecutor<TDbContext>`, `EfMessagingExtensions` (`AddOutbox<TDbContext>`, `AddInbox<TDbContext>`)
> - `Shared.Infrastructure.Messaging.Dapper/` — `INpgsqlConnectionFactory`, `DapperInboxExecutor<TFactory>`, `InboxSql`, `DapperMessagingExtensions` (`AddDapperInbox<TFactory>`)
>
> **NuGet packages:** `System.Text.Json` (SDK), `Microsoft.Extensions.Hosting.Abstractions`,
> `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`,
> `Microsoft.EntityFrameworkCore` (EF variant), `Dapper` + `Npgsql` (Dapper variant).

---

## Shared.Abstractions.Messaging — Interfaces

```csharp
// Shared.Abstractions.Messaging/IIntegrationEvent.cs
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

// Shared.Abstractions.Messaging/IIntegrationEventBus.cs
public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}

// Shared.Abstractions.Messaging/IIntegrationEventHandler.cs
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

// Shared.Abstractions.Messaging/IInboxExecutor.cs
// Idempotent execution boundary. Implementations open a transaction on the consumer's
// storage, check the inbox for the given eventId, invoke handlerInvocation only on the
// first delivery, persist the inbox marker, and commit. EF and Dapper variants live in
// the matching Shared.Infrastructure.Messaging.* packages.
public interface IInboxExecutor
{
    Task ExecuteAsync(
        Guid eventId,
        string eventType,
        Func<CancellationToken, Task> handlerInvocation,
        CancellationToken ct = default);
}
```

---

## Shared.Infrastructure.Messaging — Core

### OutboxMessage (POCO)

```csharp
// Shared.Infrastructure.Messaging/Outbox/OutboxMessage.cs
public sealed record OutboxMessage(
    Guid Id,
    Guid EventId,
    string EventType,         // assembly-qualified name
    string Payload,           // JSON
    DateTime OccurredAt,
    DateTime? ProcessedAt,
    int AttemptCount,
    string? LastError);
```

### IOutboxStore

```csharp
// Shared.Infrastructure.Messaging/Outbox/IOutboxStore.cs
public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct);
    Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct);
    Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct);
}
```

### Serializer

```csharp
// Shared.Infrastructure.Messaging/Serialization/IntegrationEventSerializer.cs
public interface IIntegrationEventSerializer
{
    string Serialize(IIntegrationEvent @event);
    IIntegrationEvent Deserialize(string payload, string eventType);
}

public sealed class IntegrationEventSerializer : IIntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.General) { PropertyNameCaseInsensitive = true };

    public string Serialize(IIntegrationEvent @event)
        => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public IIntegrationEvent Deserialize(string payload, string eventType)
    {
        var type = Type.GetType(eventType, throwOnError: true)
            ?? throw new InvalidOperationException($"Unknown event type: {eventType}");
        var result = JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException($"Failed to deserialize {eventType}");
        return (IIntegrationEvent)result;
    }
}
```

### OutboxIntegrationEventBus

```csharp
// Shared.Infrastructure.Messaging/Outbox/OutboxIntegrationEventBus.cs
public sealed class OutboxIntegrationEventBus : IIntegrationEventBus
{
    private readonly IOutboxStore _store;
    private readonly IIntegrationEventSerializer _serializer;

    public OutboxIntegrationEventBus(IOutboxStore store, IIntegrationEventSerializer serializer)
        => (_store, _serializer) = (store, serializer);

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var message = new OutboxMessage(
            Id:           Guid.NewGuid(),
            EventId:      @event.EventId,
            EventType:    typeof(TEvent).AssemblyQualifiedName!,
            Payload:      _serializer.Serialize(@event),
            OccurredAt:   @event.OccurredAt,
            ProcessedAt:  null,
            AttemptCount: 0,
            LastError:    null);

        return _store.AddAsync(message, ct);
    }
}
```

### IIntegrationEventTransport

```csharp
// Shared.Infrastructure.Messaging/Transport/IIntegrationEventTransport.cs
public interface IIntegrationEventTransport
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}
```

### InProcessIntegrationEventTransport (v1)

```csharp
// Shared.Infrastructure.Messaging/Transport/InProcessIntegrationEventTransport.cs
public sealed class InProcessIntegrationEventTransport : IIntegrationEventTransport
{
    private readonly IServiceProvider _rootProvider;

    public InProcessIntegrationEventTransport(IServiceProvider rootProvider)
        => _rootProvider = rootProvider;

    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventType = Type.GetType(message.EventType)
            ?? throw new InvalidOperationException($"Unknown event type: {message.EventType}");

        await using var scope = _rootProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var serializer = sp.GetRequiredService<IIntegrationEventSerializer>();
        var @event = serializer.Deserialize(message.Payload, message.EventType);

        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = sp.GetServices(handlerType).ToList();
        if (handlers.Count == 0) return;

        var inbox = sp.GetRequiredService<IInboxExecutor>();

        foreach (var handler in handlers)
        {
            await inbox.ExecuteAsync(
                eventId:   message.EventId,
                eventType: message.EventType,
                handlerInvocation: async invocationCt =>
                {
                    var task = (Task)handlerType
                        .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!
                        .Invoke(handler, [@event, invocationCt])!;
                    await task.ConfigureAwait(false);
                },
                ct: ct);
        }
    }
}
```

### OutboxWorker

```csharp
// Shared.Infrastructure.Messaging/Outbox/OutboxWorkerOptions.cs
public sealed class OutboxWorkerOptions
{
    public const string SectionName = "Messaging:Outbox";
    public int BatchSize { get; init; } = 50;
    public int PollIntervalMs { get; init; } = 1000;
}

// Shared.Infrastructure.Messaging/Outbox/OutboxWorker.cs
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
            try { await RunOnceAsync(stoppingToken).ConfigureAwait(false); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { _logger.LogError(ex, "Outbox worker tick failed"); }

            try { await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        // Graceful no-op until a publisher module registers an IOutboxStore (e.g. AddOutbox<TDbContext>())
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
```

### MessagingExtensions (host-level wiring)

```csharp
// Shared.Infrastructure.Messaging/Extensions/MessagingBuilder.cs
public sealed class MessagingBuilder
{
    public IServiceCollection Services { get; }
    public MessagingBuilder(IServiceCollection services) => Services = services;
}

// Shared.Infrastructure.Messaging/Extensions/MessagingExtensions.cs
public static class MessagingExtensions
{
    public static MessagingBuilder AddIntegrationEventBus(this IServiceCollection services)
    {
        services.TryAddSingleton<IIntegrationEventSerializer, IntegrationEventSerializer>();
        services.TryAddScoped<IIntegrationEventBus, OutboxIntegrationEventBus>();
        services.AddOptions<OutboxWorkerOptions>();
        services.AddHostedService<OutboxWorker>();
        return new MessagingBuilder(services);
    }

    public static MessagingBuilder UseInProcessTransport(this MessagingBuilder builder)
    {
        builder.Services.TryAddSingleton<IIntegrationEventTransport, InProcessIntegrationEventTransport>();
        return builder;
    }
}
```

---

## Shared.Infrastructure.Messaging.Ef — EF outbox + inbox

### Outbox

```csharp
// Shared.Infrastructure.Messaging.Ef/Outbox/OutboxMessageEntity.cs
public sealed class OutboxMessageEntity
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public string Payload { get; init; } = default!;
    public DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}

// Shared.Infrastructure.Messaging.Ef/Outbox/OutboxMessageEntityConfiguration.cs
public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnName("last_error");

        builder.HasIndex(x => x.ProcessedAt)
               .HasFilter("\"processed_at\" IS NULL")
               .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}

// Shared.Infrastructure.Messaging.Ef/Outbox/EfOutboxStore.cs
internal sealed class EfOutboxStore<TDbContext> : IOutboxStore
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public EfOutboxStore(TDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(OutboxMessage message, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .AddAsync(MapToEntity(message), ct).AsTask();

    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken ct)
        => await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.OccurredAt)
            .Take(batchSize)
            .Select(x => new OutboxMessage(
                x.Id, x.EventId, x.EventType, x.Payload, x.OccurredAt,
                x.ProcessedAt, x.AttemptCount, x.LastError))
            .ToListAsync(ct);

    public Task MarkProcessedAsync(Guid messageId, DateTime processedAt, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAt, processedAt), ct);

    public Task RecordFailureAsync(Guid messageId, string error, CancellationToken ct)
        => _dbContext.Set<OutboxMessageEntity>()
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LastError, error)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct);

    private static OutboxMessageEntity MapToEntity(OutboxMessage m) => new()
    {
        Id = m.Id, EventId = m.EventId, EventType = m.EventType, Payload = m.Payload,
        OccurredAt = m.OccurredAt, ProcessedAt = m.ProcessedAt,
        AttemptCount = m.AttemptCount, LastError = m.LastError
    };
}
```

### Inbox

```csharp
// Shared.Infrastructure.Messaging.Ef/Inbox/InboxMessageEntity.cs
public sealed class InboxMessageEntity
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = default!;
    public DateTime ConsumedAt { get; init; }
}

// Shared.Infrastructure.Messaging.Ef/Inbox/InboxMessageEntityConfiguration.cs
public sealed class InboxMessageEntityConfiguration : IEntityTypeConfiguration<InboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<InboxMessageEntity> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
    }
}

// Shared.Infrastructure.Messaging.Ef/Inbox/EfInboxExecutor.cs
internal sealed class EfInboxExecutor<TDbContext> : IInboxExecutor
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public EfInboxExecutor(TDbContext dbContext) => _dbContext = dbContext;

    public async Task ExecuteAsync(
        Guid eventId, string eventType,
        Func<CancellationToken, Task> handlerInvocation, CancellationToken ct = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var alreadyConsumed = await _dbContext.Set<InboxMessageEntity>()
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct)
            .ConfigureAwait(false);

        if (alreadyConsumed) { await transaction.RollbackAsync(ct).ConfigureAwait(false); return; }

        await handlerInvocation(ct).ConfigureAwait(false);

        await _dbContext.Set<InboxMessageEntity>().AddAsync(
            new InboxMessageEntity { EventId = eventId, EventType = eventType, ConsumedAt = DateTime.UtcNow },
            ct).ConfigureAwait(false);

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
```

### EF DI extensions

```csharp
// Shared.Infrastructure.Messaging.Ef/Extensions/EfMessagingExtensions.cs
public static class EfMessagingExtensions
{
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<EfOutboxStore<TDbContext>>();

        // This module's store, keyed by its DbContext type.
        services.AddKeyedScoped<IOutboxStore>(typeof(TDbContext),
            (sp, _) => sp.GetRequiredService<EfOutboxStore<TDbContext>>());

        // Unkeyed store used by OutboxIntegrationEventBus — registered once (TryAdd) and
        // shared by all publishing modules. It routes to the keyed store named by
        // OutboxScope.CurrentKey, which the DomainEventDispatcherInterceptor sets to the
        // publishing module's DbContext type for the duration of domain-event dispatch.
        // Falls back to typeof(TDbContext) for direct publishes (single-module / tests).
        services.TryAddScoped<IOutboxStore>(sp =>
        {
            var key = OutboxScope.CurrentKey ?? typeof(TDbContext);
            return sp.GetRequiredKeyedService<IOutboxStore>(key);
        });

        services.AddHostedService<OutboxWorker<TDbContext>>();
        return services;
    }

    public static IServiceCollection AddInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IInboxExecutor, EfInboxExecutor<TDbContext>>();
        return services;
    }
}
```

---

## Shared.Infrastructure.Messaging.Dapper — Dapper inbox executor

```csharp
// Shared.Infrastructure.Messaging.Dapper/INpgsqlConnectionFactory.cs
public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default);
}

// Shared.Infrastructure.Messaging.Dapper/Inbox/InboxSql.cs
internal static class InboxSql
{
    internal const string Exists = """
        SELECT EXISTS (SELECT 1 FROM inbox_messages WHERE event_id = @EventId);
        """;

    internal const string Insert = """
        INSERT INTO inbox_messages (event_id, event_type, consumed_at)
        VALUES (@EventId, @EventType, @ConsumedAt);
        """;
}

// Shared.Infrastructure.Messaging.Dapper/Inbox/DapperInboxExecutor.cs
internal sealed class DapperInboxExecutor<TFactory> : IInboxExecutor
    where TFactory : INpgsqlConnectionFactory
{
    private readonly TFactory _factory;
    public DapperInboxExecutor(TFactory factory) => _factory = factory;

    public async Task ExecuteAsync(
        Guid eventId, string eventType,
        Func<CancellationToken, Task> handlerInvocation, CancellationToken ct = default)
    {
        await using var connection = await _factory.OpenAsync(ct).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(InboxSql.Exists, new { EventId = eventId },
                transaction: transaction, cancellationToken: ct)).ConfigureAwait(false);

        if (exists) { await transaction.RollbackAsync(ct).ConfigureAwait(false); return; }

        await handlerInvocation(ct).ConfigureAwait(false);

        await connection.ExecuteAsync(
            new CommandDefinition(InboxSql.Insert,
                new { EventId = eventId, EventType = eventType, ConsumedAt = DateTime.UtcNow },
                transaction: transaction, cancellationToken: ct)).ConfigureAwait(false);

        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}

// Shared.Infrastructure.Messaging.Dapper/Extensions/DapperMessagingExtensions.cs
public static class DapperMessagingExtensions
{
    public static IServiceCollection AddDapperInbox<TFactory>(this IServiceCollection services)
        where TFactory : class, INpgsqlConnectionFactory
    {
        services.AddScoped<IInboxExecutor, DapperInboxExecutor<TFactory>>();
        return services;
    }
}
```

The handler invocation runs **inside** the Dapper inbox transaction, but the handler's
own data work uses its own scoped `DapperUnitOfWork` connection — see
`backend-dapper-module-structure.md`. v1 idempotency comes from the inbox check; the
handler is expected to be transactional on its own connection.

---

## Wiring summary

```csharp
// Host (HomeSystem.REST/Program.cs)
builder.Services.AddIntegrationEventBus().UseInProcessTransport();

// Publishing module (EF)
services.AddOutbox<BudgetPlanDbContext>();
// + DbContext applies OutboxMessageEntityConfiguration

// Consuming module (EF)
services.AddInbox<BudgetPlanDbContext>();
// + DbContext applies InboxMessageEntityConfiguration
services.AddScoped<IIntegrationEventHandler<UserDeletedIntegrationEvent>, UserDeletedIntegrationEventHandler>();

// Consuming module (Dapper)
services.AddDapperInbox<NotificationsConnectionFactory>();
// + module's DbUp migration creates inbox_messages
services.AddScoped<IIntegrationEventHandler<MealReminderDueIntegrationEvent>, MealReminderDueIntegrationEventHandler>();
```

---

## Schema (per consuming/publishing module)

```text
outbox_messages (publisher)
  id              uuid       pk
  event_id        uuid       not null   -- IIntegrationEvent.EventId
  event_type      text       not null   -- assembly-qualified name
  payload         jsonb      not null
  occurred_at     timestamptz not null
  processed_at    timestamptz null
  attempt_count   int        not null default 0
  last_error      text       null
  index on (processed_at) where processed_at is null

inbox_messages (consumer)
  event_id        uuid       pk
  event_type      text       not null
  consumed_at     timestamptz not null
```

---

## Future — RabbitMQ transport

Replaces `InProcessIntegrationEventTransport` with `RabbitMqIntegrationEventTransport`:
publishes via `BasicPublish` to a topic exchange per publishing module, consumes via a
durable queue per consumer with a dead-letter exchange. The `IInboxExecutor` contract is
unchanged — message loss / duplication semantics handled by RabbitMQ ack/nack +
inbox idempotency. Switching is one DI registration in the host
(`UseRabbitMqTransport(configuration)`); module code stays the same.
