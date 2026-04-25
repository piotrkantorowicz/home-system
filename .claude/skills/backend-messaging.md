# 11 — Messaging Infrastructure (Raw RabbitMQ)

> Complete source for the RabbitMQ messaging stack in `Shared.Abstractions` and
> `Shared.Infrastructure`. Uses `RabbitMQ.Client` directly — no MassTransit.
>
> **NuGet packages required:**
> - `RabbitMQ.Client` → `Shared.Infrastructure`
> - `System.Text.Json` (already in SDK)

---

## Shared.Abstractions — Interfaces

```csharp
// Shared.Abstractions/Messaging/IIntegrationEvent.cs
public interface IIntegrationEvent { }
```

```csharp
// Shared.Abstractions/Messaging/IIntegrationEventHandler.cs
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
```

```csharp
// Shared.Abstractions/Messaging/IEventPublisher.cs
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string exchange,
        string routingKey,
        TEvent @event,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
```

```csharp
// Shared.Abstractions/Messaging/IOutboxRepository.cs
public interface IOutboxRepository
{
    Task AddAsync(
        string exchange,
        string routingKey,
        IIntegrationEvent @event,
        CancellationToken ct = default);

    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        int batchSize,
        CancellationToken ct = default);

    Task MarkPublishedAsync(Guid messageId, CancellationToken ct = default);
}
```

```csharp
// Shared.Abstractions/Messaging/IInboxRepository.cs
public interface IInboxRepository
{
    Task<bool> ExistsAsync(Guid messageId, CancellationToken ct = default);
    Task MarkAsync(Guid messageId, CancellationToken ct = default);
}
```

```csharp
// Shared.Abstractions/Messaging/OutboxMessage.cs
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Exchange { get; init; } = default!;
    public string RoutingKey { get; init; } = default!;
    public string EventType { get; init; } = default!;      // assembly-qualified name
    public string Payload { get; init; } = default!;         // JSON
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
```

---

## Configuration

```csharp
// Shared.Infrastructure/Messaging/RabbitMqOptions.cs
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public int RetryCount { get; init; } = 3;
    public int RetryDelayMs { get; init; } = 1000;
    public int OutboxBatchSize { get; init; } = 50;
    public int OutboxIntervalMs { get; init; } = 1000;
    public int PrefetchCount { get; init; } = 10;
}
```

```json
// appsettings.json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "RetryCount": 3,
    "RetryDelayMs": 1000,
    "OutboxBatchSize": 50,
    "OutboxIntervalMs": 1000,
    "PrefetchCount": 10
  }
}
```

---

## Connection Factory

A single long-lived `IConnection` is shared across the process. Channels are short-lived
and created per publish/consume operation.

```csharp
// Shared.Infrastructure/Messaging/RabbitMqConnectionFactory.cs
public sealed class RabbitMqConnectionFactory : IDisposable
{
    private readonly IConnection _connection;
    private bool _disposed;

    public RabbitMqConnectionFactory(RabbitMqOptions options)
    {
        var factory = new ConnectionFactory
        {
            HostName    = options.Host,
            Port        = options.Port,
            VirtualHost = options.VirtualHost,
            UserName    = options.Username,
            Password    = options.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(5),
            DispatchConsumersAsync   = true
        };

        _connection = factory.CreateConnection();
    }

    public IModel CreateChannel() => _connection.CreateModel();

    public void Dispose()
    {
        if (_disposed) return;
        _connection.Close();
        _connection.Dispose();
        _disposed = true;
    }
}
```

---

## Topology Declaration

Call once at startup to ensure exchanges, queues, and bindings exist.

```csharp
// Shared.Infrastructure/Messaging/RabbitMqTopologyDeclarator.cs
public sealed class RabbitMqTopologyDeclarator
{
    private readonly RabbitMqConnectionFactory _connectionFactory;

    public RabbitMqTopologyDeclarator(RabbitMqConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    /// <summary>
    /// Declares a topic exchange, a durable queue with dead-letter support,
    /// and binds the queue to the exchange.
    /// </summary>
    public void Declare(string exchange, string queue, string routingKey)
    {
        using var channel = _connectionFactory.CreateChannel();

        // Main exchange
        channel.ExchangeDeclare(
            exchange: exchange,
            type:     ExchangeType.Topic,
            durable:  true,
            autoDelete: false);

        // Dead-letter exchange + queue
        var dlxExchange = $"{exchange}.dlx";
        var dlxQueue    = $"{queue}.dlx";

        channel.ExchangeDeclare(
            exchange:   dlxExchange,
            type:       ExchangeType.Fanout,
            durable:    true,
            autoDelete: false);

        channel.QueueDeclare(
            queue:      dlxQueue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  null);

        channel.QueueBind(dlxQueue, dlxExchange, routingKey: string.Empty);

        // Main queue with DLX configured
        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxExchange
        };

        channel.QueueDeclare(
            queue:      queue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  args);

        channel.QueueBind(
            queue:      queue,
            exchange:   exchange,
            routingKey: routingKey);
    }
}
```

---

## Event Publisher

```csharp
// Shared.Infrastructure/Messaging/RabbitMqEventPublisher.cs
internal sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqEventPublisher> _logger;

    public RabbitMqEventPublisher(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqEventPublisher> logger)
        => (_connectionFactory, _logger) = (connectionFactory, logger);

    public Task PublishAsync<TEvent>(
        string exchange,
        string routingKey,
        TEvent @event,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        // RabbitMQ.Client v6 BasicPublish is synchronous — wrap in Task.Run only if truly needed.
        // For v7+ async API use BasicPublishAsync directly.
        using var channel = _connectionFactory.CreateChannel();

        var body       = JsonSerializer.SerializeToUtf8Bytes(@event);
        var properties = channel.CreateBasicProperties();

        properties.Persistent    = true;                     // survive broker restart
        properties.ContentType   = "application/json";
        properties.Type          = typeof(TEvent).Name;      // readable message type header
        properties.MessageId     = Guid.NewGuid().ToString();
        properties.Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange:   exchange,
            routingKey: routingKey,
            mandatory:  true,                                // return if unroutable
            basicProperties: properties,
            body:       body);

        _logger.LogDebug(
            "Published {EventType} to exchange {Exchange} with routingKey {RoutingKey}",
            typeof(TEvent).Name, exchange, routingKey);

        return Task.CompletedTask;
    }
}
```

---

## Outbox Worker (BackgroundService)

Polls the outbox table and publishes pending messages to RabbitMQ.

```csharp
// Shared.Infrastructure/Messaging/OutboxWorker.cs
internal sealed class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _publisher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxWorker> _logger;

    public OutboxWorker(
        IServiceScopeFactory scopeFactory,
        IEventPublisher publisher,
        IOptions<RabbitMqOptions> options,
        ILogger<OutboxWorker> logger)
        => (_scopeFactory, _publisher, _options, _logger)
            = (scopeFactory, publisher, options.Value, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox worker encountered an error");
            }

            await Task.Delay(_options.OutboxIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var messages = await outbox.GetUnpublishedAsync(_options.OutboxBatchSize, ct);
        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.EventType)
                    ?? throw new InvalidOperationException($"Unknown event type: {message.EventType}");

                var @event = (IIntegrationEvent)JsonSerializer.Deserialize(message.Payload, eventType)!;

                await _publisher.PublishAsync(message.Exchange, message.RoutingKey, @event, ct);
                await outbox.MarkPublishedAsync(message.Id, ct);

                _logger.LogDebug("Outbox published message {MessageId} ({EventType})",
                    message.Id, message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                // Leave unprocessed — will retry on next poll
            }
        }
    }
}
```

---

## Consumer Host (BackgroundService)

One instance per queue. Deserializes the message, runs the inbox idempotency check,
then calls the registered `IIntegrationEventHandler<T>`.

```csharp
// Shared.Infrastructure/Messaging/RabbitMqConsumerHost.cs
public sealed class RabbitMqConsumerHost<TEvent> : BackgroundService
    where TEvent : class, IIntegrationEvent
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly string _queue;
    private readonly string _exchange;
    private readonly string _routingKey;
    private readonly ILogger<RabbitMqConsumerHost<TEvent>> _logger;

    public RabbitMqConsumerHost(
        IServiceScopeFactory scopeFactory,
        RabbitMqConnectionFactory connectionFactory,
        IOptions<RabbitMqOptions> options,
        string queue,
        string exchange,
        string routingKey,
        ILogger<RabbitMqConsumerHost<TEvent>> logger)
    {
        _scopeFactory       = scopeFactory;
        _connectionFactory  = connectionFactory;
        _options            = options.Value;
        _queue              = queue;
        _exchange           = exchange;
        _routingKey         = routingKey;
        _logger             = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Declare topology on startup
        var declarator = new RabbitMqTopologyDeclarator(_connectionFactory);
        declarator.Declare(_exchange, _queue, _routingKey);

        var channel = _connectionFactory.CreateChannel();
        channel.BasicQos(prefetchSize: 0, prefetchCount: (ushort)_options.PrefetchCount, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                await ConsumeAsync(ea, stoppingToken);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process message {DeliveryTag} from queue {Queue}",
                    ea.DeliveryTag, _queue);

                // requeue: false → goes to dead-letter queue
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        channel.BasicConsume(queue: _queue, autoAck: false, consumer: consumer);

        // Keep the background service alive
        stoppingToken.WaitHandle.WaitOne();
        channel.Close();
        channel.Dispose();

        return Task.CompletedTask;
    }

    private async Task ConsumeAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var messageId = Guid.TryParse(ea.BasicProperties.MessageId, out var id)
            ? id
            : Guid.NewGuid();   // fallback — should always be set by publisher

        await using var scope = _scopeFactory.CreateAsyncScope();
        var inbox   = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<TEvent>>();

        // Idempotency guard
        if (await inbox.ExistsAsync(messageId, ct))
        {
            _logger.LogDebug("Skipping duplicate message {MessageId}", messageId);
            return;
        }

        var @event = JsonSerializer.Deserialize<TEvent>(ea.Body.Span)
            ?? throw new InvalidOperationException("Failed to deserialize integration event.");

        await handler.HandleAsync(@event, ct);
        await inbox.MarkAsync(messageId, ct);

        _logger.LogDebug("Processed message {MessageId} ({EventType})", messageId, typeof(TEvent).Name);
    }
}
```

---

## Outbox & Inbox — EF Core Implementation

Both tables live in the **same** DbContext as the module that owns them.

```csharp
// Shared.Infrastructure/Messaging/OutboxRepository.cs
internal sealed class OutboxRepository : IOutboxRepository
{
    private readonly DbContext _dbContext;

    public OutboxRepository(DbContext dbContext)
        => _dbContext = dbContext;

    public async Task AddAsync(
        string exchange,
        string routingKey,
        IIntegrationEvent @event,
        CancellationToken ct = default)
    {
        var message = new OutboxMessage
        {
            Exchange   = exchange,
            RoutingKey = routingKey,
            EventType  = @event.GetType().AssemblyQualifiedName!,
            Payload    = JsonSerializer.Serialize(@event, @event.GetType())
        };

        await _dbContext.Set<OutboxMessage>().AddAsync(message, ct);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        int batchSize,
        CancellationToken ct = default)
        => await _dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task MarkPublishedAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await _dbContext.Set<OutboxMessage>().FindAsync([messageId], ct);
        if (message is not null)
            message.ProcessedAt = DateTime.UtcNow;
    }
}
```

```csharp
// Shared.Infrastructure/Messaging/InboxRepository.cs
internal sealed class InboxRepository : IInboxRepository
{
    private readonly DbContext _dbContext;

    public InboxRepository(DbContext dbContext) => _dbContext = dbContext;

    public async Task<bool> ExistsAsync(Guid messageId, CancellationToken ct = default)
        => await _dbContext.Set<InboxMessage>()
            .AnyAsync(m => m.Id == messageId, ct);

    public async Task MarkAsync(Guid messageId, CancellationToken ct = default)
        => await _dbContext.Set<InboxMessage>().AddAsync(
            new InboxMessage { Id = messageId, ProcessedAt = DateTime.UtcNow }, ct);
}

// Model
public sealed class InboxMessage
{
    public Guid Id { get; init; }
    public DateTime ProcessedAt { get; init; }
}
```

### EF Core Configurations

```csharp
// Shared.Infrastructure/Messaging/Configurations/OutboxMessageConfiguration.cs
internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Exchange).HasMaxLength(256).IsRequired();
        builder.Property(x => x.RoutingKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired(false);

        // Index to speed up the outbox worker poll query
        builder.HasIndex(x => x.ProcessedAt);
    }
}

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
```

---

## DI Registration (Shared.Infrastructure)

```csharp
// Shared.Infrastructure/Messaging/MessagingExtensions.cs
public static class MessagingExtensions
{
    /// <summary>
    /// Registers the RabbitMQ connection factory, publisher, and outbox worker.
    /// Call once in the host project.
    /// </summary>
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<RabbitMqConnectionFactory>(sp =>
            new RabbitMqConnectionFactory(
                sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value));

        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddHostedService<OutboxWorker>();

        return services;
    }

    /// <summary>
    /// Registers a consumer host for a specific event type and queue.
    /// Call once per queue in the consuming module's DI registration.
    /// </summary>
    public static IServiceCollection AddConsumer<TEvent>(
        this IServiceCollection services,
        string queue,
        string exchange,
        string routingKey)
        where TEvent : class, IIntegrationEvent
    {
        services.AddSingleton<IHostedService>(sp =>
            new RabbitMqConsumerHost<TEvent>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<RabbitMqConnectionFactory>(),
                sp.GetRequiredService<IOptions<RabbitMqOptions>>(),
                queue,
                exchange,
                routingKey,
                sp.GetRequiredService<ILogger<RabbitMqConsumerHost<TEvent>>>()));

        return services;
    }
}
```

### Host project (`Api/Program.cs`)

```csharp
// Registers the shared RabbitMQ infrastructure once
builder.Services.AddRabbitMq(builder.Configuration);
```

### Module DI (`BudgetPlan.Infrastructure/DependencyInjection.cs`)

```csharp
// Register handlers + outbox/inbox repos for this module's DbContext
services.AddScoped<IOutboxRepository>(sp =>
    new OutboxRepository(sp.GetRequiredService<BudgetPlanDbContext>()));

services.AddScoped<IInboxRepository>(sp =>
    new InboxRepository(sp.GetRequiredService<BudgetPlanDbContext>()));

// Register integration event handler
services.AddScoped<
    IIntegrationEventHandler<UserDeletedIntegrationEvent>,
    UserDeletedIntegrationEventHandler>();

// Register consumer hosted service
services.AddConsumer<UserDeletedIntegrationEvent>(
    queue:      Queues.BudgetPlan.UserDeleted,
    exchange:   Exchanges.Iam,
    routingKey: RoutingKeys.UserDeleted);
```

---

## Error Handling & Retry Strategy

| Scenario | Behaviour |
|---|---|
| Handler throws → first time | `BasicNack(requeue: false)` → dead-letter queue |
| Outbox publish fails | Message stays unprocessed; retried on next poll cycle |
| RabbitMQ connection lost | `AutomaticRecoveryEnabled = true` reconnects automatically |
| Duplicate message received | Inbox check short-circuits before handler is called |
| Unknown event type in outbox | Logged as error, skipped for that cycle |

Dead-letter queues (`.dlx` suffix) must be monitored. Set up an alert when a DLX queue depth > 0.
