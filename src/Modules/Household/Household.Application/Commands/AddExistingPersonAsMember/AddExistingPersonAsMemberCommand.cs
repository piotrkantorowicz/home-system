namespace Household.Application.Commands.AddExistingPersonAsMember;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Invites a person who already exists (linked or managed) and belongs to no household: creates a
/// 30-day pending invitation addressed to their email. Membership begins only once they explicitly
/// accept it. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="PersonId">The person to invite; must not be in any household and must have an email on file.</param>
/// <param name="Role">The role granted on acceptance.</param>
/// <param name="Nickname">Optional household-local nickname, applied when the invitation is accepted.</param>
public sealed record AddExistingPersonAsMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role,
    string? Nickname) : ICommand<AddExistingPersonAsMemberResult>;

/// <summary>Outcome of adding an existing person: the pending invitation that was created.</summary>
/// <param name="InvitationId">The pending invitation's identifier.</param>
public sealed record AddExistingPersonAsMemberResult(Guid InvitationId);
