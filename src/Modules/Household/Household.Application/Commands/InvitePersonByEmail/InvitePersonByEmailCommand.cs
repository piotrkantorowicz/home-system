using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.InvitePersonByEmail;

public sealed record InvitePersonByEmailCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Email,
    HouseholdRole Role) : ICommand<InvitePersonByEmailResult>;

/// <summary>
/// Outcome of an invite. <see cref="AddedImmediately"/> is true when a matching Person
/// already existed and was added straight away; otherwise a pending invitation was created.
/// </summary>
public sealed record InvitePersonByEmailResult(bool AddedImmediately, Guid? PersonId, Guid? InvitationId);
