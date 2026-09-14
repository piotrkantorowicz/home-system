namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a person is created, either from a first login or as a managed person.
/// </summary>
/// <param name="PersonId">The new person.</param>
/// <param name="IsManaged">True for a managed person without a login.</param>
public sealed record PersonRegisteredDomainEvent(PersonId PersonId, bool IsManaged) : IDomainEvent;
