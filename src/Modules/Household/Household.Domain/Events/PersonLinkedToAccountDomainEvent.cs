namespace Household.Domain.Events;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised when a managed person is attached to the Authentik account that signed in as them.
/// </summary>
/// <param name="PersonId">The person, whose id and history are unchanged.</param>
/// <param name="AuthSubject">The Authentik subject now linked.</param>
public sealed record PersonLinkedToAccountDomainEvent(PersonId PersonId, string AuthSubject) : IDomainEvent;
