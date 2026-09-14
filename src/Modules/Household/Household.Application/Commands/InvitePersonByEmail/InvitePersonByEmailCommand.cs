namespace Household.Application.Commands.InvitePersonByEmail;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Invites someone by email. If a person with that email already exists and has no household they are added at once; otherwise a 30-day pending invitation is created that resolves on their next sign-in. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="Email">The invitee's address; normalised before matching.</param>
/// <param name="Role">The role granted; cannot be owner.</param>
public sealed record InvitePersonByEmailCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string Email,
    HouseholdRole Role) : ICommand<InvitePersonByEmailResult>;

/// <summary>
/// Outcome of an invite. <see cref="AddedImmediately"/> is true when a matching Person
/// already existed and was added straight away; otherwise a pending invitation was created.
/// </summary>
/// <param name="AddedImmediately">Whether the person joined immediately.</param>
/// <param name="PersonId">The person added, when <paramref name="AddedImmediately"/> is true.</param>
/// <param name="InvitationId">The pending invitation, when <paramref name="AddedImmediately"/> is false.</param>
public sealed record InvitePersonByEmailResult(bool AddedImmediately, Guid? PersonId, Guid? InvitationId);
