# Backend — CQRS Patterns (Custom Dispatcher)

> This project uses a **custom CQRS implementation** — no MediatR.
> Contracts live in `Shared.Abstractions.Cqrs`; implementations in `Shared.Infrastructure.Cqrs`
> (dispatchers, decorators, DI registration). See `.claude/skills/backend-cqrs.md` for the full
> infrastructure source.

---

## Core Concept

Commands and queries are dispatched through typed interfaces resolved from the DI container.
Cross-cutting concerns (logging, validation, transactions) are implemented as **dispatcher decorators**,
not pipeline behaviours.

```
Endpoint
  → ICommandDispatcher.SendAsync<TCommand, TResult>(command, ct)
      → decorator chain: logging → validation → transaction
          → ICommandHandler<TCommand, TResult>.HandleAsync(command, ct)
```

---

## Commands

### Command Record

```csharp
// Application/Commands/CreateBudgetPlan/CreateBudgetPlanCommand.cs
public sealed record CreateBudgetPlanCommand(
    Guid UserId,
    decimal LimitValue,
    string LimitCurrency) : ICommand<Guid>;   // returns the new plan's ID
```

Use `ICommand<TResult>` when you need to return something (e.g. a new ID).
Use `ICommand` (no result) for mutations where the caller needs nothing back.

### Command Handler

```csharp
// Application/Commands/CreateBudgetPlan/CreateBudgetPlanCommandHandler.cs
// Primary constructor — no fields, no tuple assignment (see coding standards)
internal sealed class CreateBudgetPlanCommandHandler(
    IBudgetPlanRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<CreateBudgetPlanCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateBudgetPlanCommand command, CancellationToken ct)
    {
        var id = BudgetPlanId.New();
        var now = clock.GetUtcNow();

        var plan = BudgetPlan.Create(
            id,
            new Money(command.LimitValue, command.LimitCurrency),
            DateRange.MonthOf(now),
            now);

        await repository.AddAsync(plan, ct);
        await unitOfWork.CommitAsync(ct);

        return id.Value;
    }
}
```

### Command Validator

```csharp
// Application/Commands/CreateBudgetPlan/CreateBudgetPlanCommandValidator.cs
internal sealed class CreateBudgetPlanCommandValidator
    : ICommandValidator<CreateBudgetPlanCommand>
{
    public IEnumerable<ValidationError> Validate(CreateBudgetPlanCommand command)
    {
        if (command.UserId == Guid.Empty)
            yield return new ValidationError(nameof(command.UserId), "UserId must not be empty.");

        if (command.LimitValue <= 0)
            yield return new ValidationError(nameof(command.LimitValue), "Limit must be positive.");

        if (string.IsNullOrWhiteSpace(command.LimitCurrency) || command.LimitCurrency.Length != 3)
            yield return new ValidationError(nameof(command.LimitCurrency), "Currency must be a 3-letter ISO 4217 code.");
    }
}
```

Validators are picked up automatically by the `ValidationCommandDispatcherDecorator`.
See `.claude/skills/backend-cqrs.md` for the decorator implementation.

---

## Queries

### Query Record

```csharp
// Application/Queries/GetBudgetPlan/GetBudgetPlanQuery.cs
public sealed record GetBudgetPlanQuery(Guid BudgetPlanId) : IQuery<BudgetPlanDto?>;
```

### DTO

```csharp
// Application/Queries/GetBudgetPlan/BudgetPlanDto.cs
public sealed record BudgetPlanDto(
    Guid Id,
    decimal LimitValue,
    string LimitCurrency,
    int EntryCount,
    string Status);
```

### Query Handler

```csharp
// Application/Queries/GetBudgetPlan/GetBudgetPlanQueryHandler.cs
internal sealed class GetBudgetPlanQueryHandler(BudgetPlanDbContext dbContext)
    : IQueryHandler<GetBudgetPlanQuery, BudgetPlanDto?>
{
    public async Task<BudgetPlanDto?> HandleAsync(GetBudgetPlanQuery query, CancellationToken ct)
        => await dbContext.BudgetPlans
            .AsNoTracking()
            .Where(x => x.Id == BudgetPlanId.From(query.BudgetPlanId))
            .Select(x => new BudgetPlanDto(
                x.Id.Value,
                x.Limit.Value,
                x.Limit.Currency,
                x.Entries.Count,
                x.Status.ToString()))
            .FirstOrDefaultAsync(ct);
}
```

---

## Mapping — No AutoMapper

Map manually with a static extension or a dedicated mapper class in the Application layer.

```csharp
// Application/EventHandlers/BudgetPlanMapper.cs
internal static class BudgetPlanMapper
{
    // Domain event → integration event (used in domain event handler)
    internal static BudgetPlanCreatedIntegrationEvent ToIntegrationEvent(
        this BudgetPlanCreatedDomainEvent @event, DateTimeOffset now)
        => new(
            EventId: Guid.CreateVersion7(),
            OccurredAt: now.UtcDateTime,
            BudgetPlanId: @event.BudgetPlanId.Value);

    // Aggregate → summary DTO (only add when projection is complex or reused across queries)
    internal static BudgetPlanDto ToDto(this BudgetPlan plan)
        => new(
            Id: plan.Id.Value,
            LimitValue: plan.Limit.Value,
            LimitCurrency: plan.Limit.Currency,
            EntryCount: plan.Entries.Count,
            Status: plan.Status.ToString());
}
```

### Mapping rules

- **Never use AutoMapper.** Map explicitly so the transformation is always visible.
- Read-side projections (Style 1): use EF `Select()` inline in the query handler — no aggregate is loaded.
- Read-side projections (Style 2): use Dapper `QueryAsync<TDto>` in the query handler — see `backend-dapper-module-structure.md`.
- Write-side mappings (domain event → integration event): live in `Application/EventHandlers/`.
- Request → command: mapped inline in the endpoint method body.
- `ToDto()` mapper methods on aggregates: only add when the same projection is needed in more than one place.

---

## Domain Event Handler

Domain events are dispatched synchronously after `SaveChanges` via the
`DomainEventDispatcherInterceptor` in `Shared.Infrastructure.Persistence`. Handlers map the
domain event to an integration event and publish it via `IIntegrationEventBus` — the bus
writes to the module's outbox in the same EF Core transaction.

```csharp
// Application/EventHandlers/BudgetPlanCreatedDomainEventHandler.cs
internal sealed class BudgetPlanCreatedDomainEventHandler(IIntegrationEventBus bus, TimeProvider clock)
    : IDomainEventHandler<BudgetPlanCreatedDomainEvent>
{
    public Task HandleAsync(BudgetPlanCreatedDomainEvent domainEvent, CancellationToken ct)
        => bus.PublishAsync(domainEvent.ToIntegrationEvent(clock.GetUtcNow()), ct);
}
```

See `backend-integration-patterns.md` for the publishing flow and the in-process / future
RabbitMQ transports.

---

## Using the Dispatcher in Endpoints

```csharp
// BudgetPlan.Api/BudgetPlanEndpoints.cs
// Typed results — OpenAPI is inferred from the signature (see backend-api-patterns.md)
private static async Task<Created> CreateBudgetPlan(
    CreateBudgetPlanRequest request,
    ICommandDispatcher dispatcher,
    CancellationToken ct)
{
    var id = await dispatcher.SendAsync<CreateBudgetPlanCommand, Guid>(
        new CreateBudgetPlanCommand(request.UserId, request.LimitValue, request.LimitCurrency),
        ct);

    return TypedResults.Created($"/api/budget-plans/{id}");
}

private static async Task<Results<Ok<BudgetPlanDto>, NotFound>> GetBudgetPlan(
    Guid id,
    IQueryDispatcher dispatcher,
    CancellationToken ct)
{
    var result = await dispatcher.SendAsync<GetBudgetPlanQuery, BudgetPlanDto?>(
        new GetBudgetPlanQuery(id),
        ct);

    return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
}
```

---

## Rules Summary

| Rule | Detail |
|---|---|
| Handlers are `internal sealed` with a primary constructor | Never public, never inherited, no `_field` boilerplate |
| Time via `TimeProvider` | Never `DateTime.UtcNow` in a handler |
| Commands own one `CommitAsync` | Never commit multiple times in one handler |
| Queries use `AsNoTracking()` + `Select()` | Never load a full aggregate for a read |
| No AutoMapper | Map explicitly with static methods or inline `Select()` |
| Validators are separate classes | Never inline validation in handlers |
| Dispatchers injected into endpoints | Never inject `ICommandHandler<,>` directly |
| Domain event handlers publish via `IIntegrationEventBus` | The bus writes to the module's outbox in the same transaction |
