namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Base class for aggregate roots: the consistency boundary that owns its child entities and
/// records the domain events raised while it is mutated. Events accumulate in memory and are
/// dispatched by the persistence layer's <c>DomainEventDispatcherInterceptor</c> during
/// <c>SaveChanges</c>, <em>before</em> the aggregate is flushed to the database and inside the same
/// transaction — never by the aggregate itself. A handler therefore sees the aggregate's new state
/// in memory but not yet in the database, and anything it writes (e.g. an outbox row) commits or
/// rolls back together with the aggregate.
/// </summary>
/// <typeparam name="TId">The typed identifier record of the aggregate (e.g. <c>HouseholdId</c>).</typeparam>
public abstract class AggregateRoot<TId> : IAggregateRoot
{
    /// <summary>
    /// Parameterless constructor required by EF Core for materialisation. Derived aggregates keep
    /// theirs <c>private</c> and expose a static <c>Create</c> factory as the only public creation path.
    /// </summary>
    protected AggregateRoot() { }

    /// <summary>
    /// The aggregate's identity. Assigned once by the factory method; the setter is protected so
    /// only the aggregate and EF Core can populate it.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised since the aggregate was loaded or since the last
    /// <see cref="ClearDomainEvents"/>, in the order they were raised.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records a domain event to be dispatched during the next <c>SaveChanges</c>, before the flush
    /// and inside the same transaction. Call it from the mutation method that changed the state the
    /// event describes.
    /// </summary>
    /// <param name="domainEvent">The event describing what just happened inside this aggregate.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Drops all pending domain events. The dispatcher calls it after capturing the events and before
    /// invoking their handlers, so a save that fails part-way or a second save does not replay them.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
