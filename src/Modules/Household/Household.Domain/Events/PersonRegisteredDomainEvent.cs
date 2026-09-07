namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record PersonRegisteredDomainEvent(PersonId PersonId, bool IsManaged) : IDomainEvent;
