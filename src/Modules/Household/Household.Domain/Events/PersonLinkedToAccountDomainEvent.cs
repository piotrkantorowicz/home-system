namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed record PersonLinkedToAccountDomainEvent(PersonId PersonId, string AuthSubject) : IDomainEvent;
