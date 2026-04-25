# Backend — Integration Patterns (Cross-Module & Messaging)

> v1 transport is **in-process** (`InProcessIntegrationEventTransport`) for simplicity.
> The transport seam (`IIntegrationEventTransport`) lets us drop in a RabbitMQ implementation
> later without touching any module code. See `.claude/skills/backend-messaging.md` for the
> shared messaging stack source.

---

## The Core Rule

Treat every module boundary as if it were a **network boundary between microservices**.
Another module's internals are as inaccessible as another service's database.

## Allowed Across Module Boundaries

| What | How |
|---|---|
| React to something another module did | Implement `IIntegrationEventHandler<TEvent>` for an event from its `Contracts` project |
| Call a public query from another module | Inject its `IXxxQueryService` from the Contracts project |
| Reference a shared primitive (UserId, Money) | Put it in `Shared.Abstractions.Core` |
| Pass startup config between modules | Options pattern |

## Forbidden Across Module Boundaries

```
❌ Importing ModuleA.Domain from ModuleB
❌ Injecting ModuleA's repository into ModuleB's handler
❌ Querying ModuleA's DbContext from ModuleB
❌ Sharing EF Core entities across modules
❌ Synchronous in-process HTTP calls between modules (use events or Contracts interface)
❌ Publishing to the bus directly from a transport — always go through IIntegrationEventBus → outbox
```

---

## End-to-End Flow (Outbox → Transport → Inbox → Handler)

```
[Write side — within one DB transaction]
  CommandHandler
    → Aggregate mutates + raises DomainEvent
    → DomainEventDispatcherInterceptor invokes IDomainEventHandler
    → Handler calls IIntegrationEventBus.PublishAsync(integrationEvent)
        → OutboxIntegrationEventBus → IOutboxStore.AddAsync
            (EF: outbox row tracked in the same DbContext as the aggregate)
    → IUnitOfWork.CommitAsync
        → aggregate row + outbox row committed atomically

[Outbox worker — BackgroundService, every ~1 s]
    → IOutboxStore.GetUnprocessedAsync(batchSize)
    → for each message: IIntegrationEventTransport.DispatchAsync(message)
    → on success: IOutboxStore.MarkProcessedAsync
    → on failure: IOutboxStore.RecordFailureAsync (increments attempt_count)

[Transport — InProcessIntegrationEventTransport (v1)]
    → resolves IIntegrationEventHandler<T> from a fresh DI scope
    → invokes IInboxExecutor.ExecuteAsync(eventId, eventType, handlerInvocation)
        → executor opens a transaction on the consuming module's storage
        → checks inbox_messages for eventId  ← idempotency check
        → if absent: invokes the handler, inserts inbox row, commits
        → if present: rolls back, no-op (duplicate-safe)

[RabbitMQ transport — future, identical contract]
    → BasicPublish on a topic exchange + module-bound queues + DLX
    → Consumer host calls the same IInboxExecutor on the consuming module
```

This guarantees **at-least-once delivery with idempotent consumers**, no distributed
transactions, no dual-write risk.

---

## Integration Events (Contracts Project)

```csharp
// BudgetPlan.Contracts/Events/BudgetPlanCreatedIntegrationEvent.cs
public sealed record BudgetPlanCreatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    Guid BudgetPlanId,
    Guid UserId,
    decimal LimitValue,
    string LimitCurrency) : IIntegrationEvent;
```

Rules:
- Integration events live in the **Contracts** project — never Domain or Infrastructure.
- Plain C# records — primitive types only, no domain types.
- They implement `IIntegrationEvent` and **must include `EventId` and `OccurredAt`** (the
  interface requires them). `EventId` is what the inbox checks for idempotency.
- Immutable — all properties `init`-only via the record primary constructor.

---

## Publishing — Domain Event Handler → Bus

The domain-event handler maps the domain event to an integration event and publishes it
via `IIntegrationEventBus`. The bus writes to the module's outbox in the same EF Core
transaction.

```csharp
// BudgetPlan.Application/EventHandlers/BudgetPlanCreatedDomainEventHandler.cs
internal sealed class BudgetPlanCreatedDomainEventHandler
    : IDomainEventHandler<BudgetPlanCreatedDomainEvent>
{
    private readonly IIntegrationEventBus _bus;

    public BudgetPlanCreatedDomainEventHandler(IIntegrationEventBus bus)
        => _bus = bus;

    public Task HandleAsync(BudgetPlanCreatedDomainEvent domainEvent, CancellationToken ct)
    {
        var integrationEvent = new BudgetPlanCreatedIntegrationEvent(
            EventId:      Guid.NewGuid(),
            OccurredAt:   DateTime.UtcNow,
            BudgetPlanId: domainEvent.BudgetPlanId.Value,
            UserId:       domainEvent.UserId.Value,
            LimitValue:   domainEvent.Limit.Value,
            LimitCurrency: domainEvent.Limit.Currency);

        return _bus.PublishAsync(integrationEvent, ct);
    }
}
```

The handler never touches a transport — it only calls `PublishAsync`. The transport is
chosen by host wiring (`UseInProcessTransport()` today, `UseRabbitMqTransport()` later).

---

## Consuming — Integration Event Handler

Each consumer module implements `IIntegrationEventHandler<TEvent>` for events it cares
about. The transport invokes the handler inside `IInboxExecutor.ExecuteAsync`, so the
handler does **not** need to do its own idempotency check — but its own work should be
transactional.

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
- Idempotency is provided by the inbox executor — the handler doesn't repeat the check.
- If the handler throws, the inbox transaction rolls back (no inbox row written) and the
  outbox worker records a failure → next tick retries.
- If the work is complex, dispatch a command via `ICommandDispatcher` instead of putting
  logic in the handler.

---

## Module Wiring

### Host (registers the bus + transport once)

```csharp
// HomeSystem.REST/Program.cs
builder.Services
    .AddIntegrationEventBus()       // serializer + bus + outbox worker (graceful no-op if no store)
    .UseInProcessTransport();        // v1; later: .UseRabbitMqTransport(builder.Configuration)
```

### Publishing module (EF — registers an outbox store)

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
services.AddOutbox<BudgetPlanDbContext>();
```

This binds `IOutboxStore` to `EfOutboxStore<BudgetPlanDbContext>` so `IIntegrationEventBus`
writes outbox rows in the module's DbContext, atomic with the aggregate write.

The module's `DbContext` must `ApplyConfiguration(new OutboxMessageEntityConfiguration())`
so the `outbox_messages` table is part of its schema and migrations.

### Consuming module (EF — registers the inbox executor + handlers)

```csharp
// BudgetPlan.Infrastructure/DependencyInjection.cs
services.AddInbox<BudgetPlanDbContext>();

services.AddScoped<
    IIntegrationEventHandler<UserDeletedIntegrationEvent>,
    UserDeletedIntegrationEventHandler>();
```

The DbContext must also apply `InboxMessageEntityConfiguration` so the
`inbox_messages` table exists.

### Consuming module (Dapper — Style 2)

```csharp
// Notifications.Infrastructure/DependencyInjection.cs
services.AddDapperInbox<NotificationsConnectionFactory>();

services.AddScoped<
    IIntegrationEventHandler<MealReminderDueIntegrationEvent>,
    MealReminderDueIntegrationEventHandler>();
```

The Dapper inbox executor uses the module's `INpgsqlConnectionFactory` — see
`backend-dapper-module-structure.md`.

---

## Synchronous Cross-Module Contract (When Truly Needed)

Only use this when an event-driven approach is impractical — e.g. you need a synchronous
answer during request processing, not just a side effect.

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

The consuming module (BudgetPlan) only references `IAM.Contracts` — never `IAM.Domain`
or `IAM.Infrastructure`.

---

## Future — RabbitMQ transport

Drops in by registering a different `IIntegrationEventTransport` implementation in the
host. Module code (publishers + consumers) is unchanged. RabbitMQ-specific concerns
(topic exchange per publishing module, durable queues per consumer, dead-letter
exchanges, persistent messages) live entirely inside that transport implementation.
