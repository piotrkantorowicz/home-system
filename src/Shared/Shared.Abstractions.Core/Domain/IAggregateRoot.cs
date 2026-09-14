namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Non-generic view of an aggregate root used by the persistence layer to collect and clear
/// pending domain events without knowing the aggregate's identifier type.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Domain events raised on this aggregate that have not been dispatched yet.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Drops all pending domain events; called by the dispatcher once it has captured them, before handlers run.</summary>
    void ClearDomainEvents();
}
