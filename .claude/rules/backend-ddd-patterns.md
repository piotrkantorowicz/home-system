# 04 — DDD Tactical Patterns

## Aggregate Root

```csharp
public sealed class BudgetPlan : AggregateRoot<BudgetPlanId>
{
    private readonly List<BudgetEntry> _entries = [];

    private BudgetPlan() { }   // Required by EF Core — keep private

    // Factory method is the only public creation path
    public static BudgetPlan Create(BudgetPlanId id, Money limit, DateRange period)
    {
        ArgumentNullException.ThrowIfNull(limit);
        ArgumentNullException.ThrowIfNull(period);

        var plan = new BudgetPlan
        {
            Id = id,
            Limit = limit,
            Period = period,
            Status = BudgetPlanStatus.Active
        };

        plan.RaiseDomainEvent(new BudgetPlanCreatedDomainEvent(id));
        return plan;
    }

    public Money Limit { get; private set; } = default!;
    public DateRange Period { get; private set; } = default!;
    public BudgetPlanStatus Status { get; private set; }
    public IReadOnlyCollection<BudgetEntry> Entries => _entries.AsReadOnly();

    public void AddEntry(Money amount, string description)
    {
        EnsureIsActive();

        var currentTotal = _entries.Aggregate(Money.Zero(Limit.Currency), (sum, e) => sum.Add(e.Amount));
        if (currentTotal.Add(amount).Value > Limit.Value)
            throw new BudgetPlanDomainException("Adding this entry would exceed the budget limit.");

        var entry = BudgetEntry.Create(BudgetEntryId.New(), amount, description);
        _entries.Add(entry);
        RaiseDomainEvent(new BudgetEntryAddedDomainEvent(Id, entry.Id, amount));
    }

    public void Close()
    {
        EnsureIsActive();
        Status = BudgetPlanStatus.Closed;
        RaiseDomainEvent(new BudgetPlanClosedDomainEvent(Id));
    }

    private void EnsureIsActive()
    {
        if (Status != BudgetPlanStatus.Active)
            throw new BudgetPlanDomainException("Operation is only allowed on active budget plans.");
    }
}
```

### Aggregate Rules
- **No public setters** — ever.
- **Private parameterless constructor** required for EF Core; keep it private.
- **Static `Create()` factory method** is the only way to construct a valid aggregate.
- **Raise domain events** inside mutation methods — never publish to the bus directly.
- Keep aggregates **small**. More than ~4 child collection types = wrong boundary.
- Aggregates should be **loadable without joins** where possible; avoid deep object graphs.

## Entity

```csharp
public sealed class BudgetEntry : Entity<BudgetEntryId>
{
    private BudgetEntry() { }

    internal static BudgetEntry Create(BudgetEntryId id, Money amount, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return new BudgetEntry { Id = id, Amount = amount, Description = description };
    }

    public Money Amount { get; private set; } = default!;
    public string Description { get; private set; } = default!;
}
```

- Entities inside an aggregate are **`internal`** — not accessible outside the Domain project.
- Only the aggregate root should create its own child entities.

## Value Object

```csharp
// Records give structural equality for free — ideal for VOs
public sealed record Money(decimal Value, string Currency)
{
    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException("Cannot add amounts in different currencies.");
        return new Money(Value + other.Value, Currency);
    }
}

public sealed record DateRange(DateOnly Start, DateOnly End)
{
    public static DateRange CurrentMonth()
    {
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DateRange(new DateOnly(now.Year, now.Month, 1),
                             new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)));
    }

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
```

### Value Object Rules
- Always use a **record** — never a mutable class.
- **No identity** — equality is structural (by value, not by ID).
- Methods return **new instances** — never mutate.
- Validate in the constructor: throw `DomainException` for invalid state.

## Typed IDs

```csharp
// Every aggregate root gets a typed ID record — never a raw Guid
public sealed record BudgetPlanId(Guid Value)
{
    public static BudgetPlanId New() => new(Guid.NewGuid());
    public static BudgetPlanId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}

public sealed record BudgetEntryId(Guid Value)
{
    public static BudgetEntryId New() => new(Guid.NewGuid());
    public static BudgetEntryId From(Guid value) => new(value);
}
```

Typed IDs prevent `(Guid budgetPlanId, Guid userId)` parameter swaps at compile time.

## Domain Events

```csharp
// Domain event = something that happened within this bounded context
// Lives in Domain project. Uses only domain types.
public sealed record BudgetPlanCreatedDomainEvent(BudgetPlanId BudgetPlanId) : IDomainEvent;
public sealed record BudgetEntryAddedDomainEvent(
    BudgetPlanId BudgetPlanId,
    BudgetEntryId EntryId,
    Money Amount) : IDomainEvent;
```

Domain events are **internal** to the module — never exposed in Contracts.

## Integration Events

```csharp
// Integration event = published to the bus for other modules to consume
// Lives in Contracts project. Plain C# records. No domain types — use primitives only.
public sealed record BudgetPlanCreatedIntegrationEvent(
    Guid BudgetPlanId,
    Guid UserId,
    decimal LimitValue,
    string LimitCurrency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime OccurredAt) : IIntegrationEvent;
```

## Domain Service

Use a Domain Service **only when** a domain operation involves multiple aggregates and the logic
doesn't naturally belong in any single one.

```csharp
// Example: transferring budget between two plans requires both aggregates
public sealed class BudgetTransferService
{
    public void Transfer(BudgetPlan source, BudgetPlan destination, Money amount)
    {
        source.Withdraw(amount);
        destination.Deposit(amount);
    }
}
```

If you're reaching for a Domain Service frequently, consider whether the aggregate boundaries are wrong.

## Repository Interface

```csharp
// Defined in Domain project — no EF Core references here
public interface IBudgetPlanRepository
{
    Task<BudgetPlan?> GetByIdAsync(BudgetPlanId id, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetPlan>> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task AddAsync(BudgetPlan plan, CancellationToken ct = default);
    void Update(BudgetPlan plan);
    void Delete(BudgetPlan plan);
}
```

- `GetById` returns **nullable** — not-found is expected.
- `Update` and `Delete` are **synchronous** — they just mark the EF Core entity; the actual DB call happens in `UnitOfWork.CommitAsync()`.
- Do **not** expose `IQueryable<T>` from repositories. Queries go through DbContext directly.

## Domain Exceptions

```csharp
// Base domain exception — in Shared.Abstractions or module Domain
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// Module-specific — in BudgetPlan.Domain
public sealed class BudgetPlanDomainException : DomainException
{
    public BudgetPlanDomainException(string message) : base(message) { }
}
```

- Domain exceptions carry **business language** — no stack traces, no EF/SQL detail.
- Infrastructure exceptions are let through and handled at the API boundary middleware.

## Base Types in Shared.Abstractions

```csharp
public abstract class AggregateRoot<TId>
{
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
}

public interface IDomainEvent { }
public interface IIntegrationEvent { }
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct = default);
}
```
