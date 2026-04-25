# 07 — Integration Patterns (Cross-Module & Messaging)

> This project uses **raw RabbitMQ** via the official `RabbitMQ.Client` library — no MassTransit.
> Full infrastructure source (publisher, consumer host, outbox worker) is in
> `@docs/claude/11-messaging-infrastructure.md`.

---

## The Core Rule

Treat every module boundary as if it were a **network boundary between microservices**.
Another module's internals are as inaccessible as another service's database.

## Allowed Across Module Boundaries

| What | How |
|---|---|
| React to something another module did | Subscribe to its integration event via a RabbitMQ consumer |
| Call a public query from another module | Inject its `IXxxQueryService` from the Contracts project |
| Reference a shared primitive (UserId, Money) | Put it in `Shared.Abstractions` |
| Pass startup config between modules | Options pattern |

## Forbidden Across Module Boundaries

```
❌ Importing ModuleA.Domain from ModuleB
❌ Injecting ModuleA's repository into ModuleB's handler
❌ Querying ModuleA's DbContext from ModuleB
❌ Sharing EF Core entities across modules
❌ Synchronous in-process HTTP calls between modules (use events or Contracts interface)
```

---

## End-to-End Flow (Outbox → RabbitMQ → Consumer)

```
[Write side — within one DB transaction]
  CommandHandler
    → Aggregate mutates + raises DomainEvent
    → DomainEventHandler maps DomainEvent → IntegrationEvent
    → OutboxRepository.AddAsync(serialized IntegrationEvent)   ← same transaction
    → UnitOfWork.CommitAsync()
        → aggregate row + outbox row committed atomically

[Outbox worker — BackgroundService, every ~1 s]
    → SELECT unpublished outbox rows (ProcessedAt IS NULL)
    → deserialize IntegrationEvent
    → IEventPublisher.PublishAsync(exchange, routingKey, event)
        → RabbitMQ BasicPublish (persistent, mandatory)
    → UPDATE outbox row: ProcessedAt = UtcNow
    → commit

[Consumer — BackgroundService per queue]
    → RabbitMQ BasicConsume on module's queue
    → deserialize message
    → IInboxRepository.ExistsAsync(messageId)  ← idempotency check
    → dispatch to IIntegrationEventHandler<T>
    → IInboxRepository.MarkAsync(messageId)
    → UnitOfWork.CommitAsync()
    → BasicAck
    (on failure → BasicNack with requeue=false → dead-letter queue)
```

This guarantees **at-least-once delivery** without distributed transactions or dual-write risk.

---

## Exchange & Queue Topology

Use a **topic exchange** per publishing module. Consuming modules bind their own durable queue
to the exchange with a routing key matching the event type.

```
Exchange:  budgetplan.events          (type=topic, durable)
  Binding: routingKey = BudgetPlanCreated  →  Queue: notifications.budgetplan-created
  Binding: routingKey = BudgetPlanClosed   →  Queue: reporting.budgetplan-closed

Exchange:  iam.events                 (type=topic, durable)
  Binding: routingKey = UserDeleted        →  Queue: budgetplan.user-deleted
  Binding: routingKey = UserDeleted        →  Queue: notifications.user-deleted
```

Rules:
- **One exchange per publishing module.** Name: `{modulename}.events` (lowercase).
- **One queue per (consuming module, event type) pair.** Name: `{consumer}.{eventtype}` (lowercase, kebab-case).
- **All exchanges and queues are durable.** Messages are persistent (`DeliveryMode = 2`).
- Every queue has a **dead-letter exchange** (`{queuename}.dlx`) for poison messages.
- Routing key = the integration event class name (e.g. `BudgetPlanCreated`).

### Constants

```csharp
// Shared.Infrastructure/Messaging/MessagingConstants.cs
public static class Exchanges
{
    public const string BudgetPlan = "budgetplan.events";
    public const string Iam        = "iam.events";
    // add one per publishing module
}

public static class Queues
{
    public static class BudgetPlan
    {
        public const string UserDeleted = "budgetplan.user-deleted";
    }

    public static class Notifications
    {
        public const string BudgetPlanCreated = "notifications.budgetplan-created";
    }
}

public static class RoutingKeys
{
    public const string BudgetPlanCreated = nameof(BudgetPlanCreatedIntegrationEvent);
    public const string BudgetPlanClosed  = nameof(BudgetPlanClosedIntegrationEvent);
    public const string UserDeleted       = nameof(UserDeletedIntegrationEvent);
}
```

Never use magic strings — always reference constants from `MessagingConstants`.

---

## Integration Events (Contracts Project)

```csharp
// BudgetPlan.Contracts/Events/BudgetPlanCreatedIntegrationEvent.cs
public sealed record BudgetPlanCreatedIntegrationEvent(
    Guid BudgetPlanId,
    Guid UserId,
    decimal LimitValue,
    string LimitCurrency,
    DateTime OccurredAt) : IIntegrationEvent;
```

Rules:
- Integration events are **plain C# records** — primitive types only, no domain types.
- They live in the **Contracts** project — never in Domain or Infrastructure.
- They are **immutable** — all properties `init`-only via the record primary constructor.
- Include `OccurredAt` (UTC) on every event for ordering and debugging.

---

## Publishing — Domain Event Handler → Outbox

```csharp
// BudgetPlan.Application/EventHandlers/BudgetPlanCreatedDomainEventHandler.cs
internal sealed class BudgetPlanCreatedDomainEventHandler
    : IDomainEventHandler<BudgetPlanCreatedDomainEvent>
{
    private readonly IOutboxRepository _outbox;

    public BudgetPlanCreatedDomainEventHandler(IOutboxRepository outbox)
        => _outbox = outbox;

    public async Task HandleAsync(BudgetPlanCreatedDomainEvent domainEvent, CancellationToken ct)
    {
        var integrationEvent = new BudgetPlanCreatedIntegrationEvent(
            BudgetPlanId: domainEvent.BudgetPlanId.Value,
            UserId: domainEvent.UserId.Value,
            LimitValue: domainEvent.Limit.Value,
            LimitCurrency: domainEvent.Limit.Currency,
            OccurredAt: DateTime.UtcNow);

        await _outbox.AddAsync(
            exchange:    Exchanges.BudgetPlan,
            routingKey:  RoutingKeys.BudgetPlanCreated,
            @event:      integrationEvent,
            ct:          ct);
    }
}
```

The handler writes to the outbox only — it never touches the RabbitMQ channel directly.

---

## Consuming — Integration Event Handler

Each consumer module implements `IIntegrationEventHandler<T>` for the events it cares about.

```csharp
// BudgetPlan.Infrastructure/Messaging/Handlers/UserDeletedIntegrationEventHandler.cs
internal sealed class UserDeletedIntegrationEventHandler
    : IIntegrationEventHandler<UserDeletedIntegrationEvent>
{
    private readonly IBudgetPlanRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserDeletedIntegrationEventHandler(
        IBudgetPlanRepository repository,
        IUnitOfWork unitOfWork)
        => (_repository, _unitOfWork) = (repository, unitOfWork);

    public async Task HandleAsync(UserDeletedIntegrationEvent @event, CancellationToken ct)
    {
        var plans = await _repository.GetByUserIdAsync(new UserId(@event.UserId), ct);

        foreach (var plan in plans)
            _repository.Delete(plan);

        await _unitOfWork.CommitAsync(ct);
    }
}
```

Rules:
- Handlers are `internal sealed`.
- Handlers are **idempotent** — the inbox check happens in the consumer host before calling the handler.
- If the handler throws, the consumer host NACKs the message → it goes to the dead-letter queue.
- If complex logic is needed, call a command via `ICommandDispatcher` — don't bloat the handler.

---

## Registering Consumers per Module

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
internal static IServiceCollection AddBudgetPlanMessaging(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Register integration event handlers
    services.AddScoped<IIntegrationEventHandler<UserDeletedIntegrationEvent>,
                       UserDeletedIntegrationEventHandler>();

    // Register a consumer host for each queue this module listens to
    services.AddSingleton<IHostedService>(sp =>
        new RabbitMqConsumerHost<UserDeletedIntegrationEvent>(
            sp,
            queue:      Queues.BudgetPlan.UserDeleted,
            exchange:   Exchanges.Iam,
            routingKey: RoutingKeys.UserDeleted,
            options:    sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value));

    return services;
}
```

---

## Synchronous Cross-Module Contract (When Truly Needed)

Only use this when an event-driven approach is impractical — e.g. you need a synchronous answer
during request processing, not just a side effect.

```csharp
// In IAM.Contracts/Interfaces/IUserContextService.cs
public interface IUserContextService
{
    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserDto(Guid Id, string Email, string DisplayName);
```

```csharp
// In IAM.Infrastructure: implement IUserContextService against IAM's own DbContext
// In BudgetPlan.Application: inject IUserContextService — no reference to IAM internals
```

The consuming module (BudgetPlan) only references `IAM.Contracts` — never `IAM.Domain` or
`IAM.Infrastructure`.
