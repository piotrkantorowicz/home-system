namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Marker for something that happened inside one bounded context. Domain events are immutable
/// records that use domain types, stay internal to their module, and are handled synchronously in
/// the same transaction as the aggregate change that raised them. Anything other modules need to
/// know about is republished as an integration event by a domain-event handler.
/// </summary>
public interface IDomainEvent { }
