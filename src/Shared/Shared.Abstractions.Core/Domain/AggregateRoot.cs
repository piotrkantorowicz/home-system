namespace Shared.Abstractions.Core.Domain;

public abstract class AggregateRoot<TId> : IAggregateRoot
{
    protected AggregateRoot() { }   // Required by EF Core for materialisation

    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
