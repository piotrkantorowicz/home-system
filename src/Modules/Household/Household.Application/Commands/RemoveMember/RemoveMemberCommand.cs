namespace Household.Application.Commands.RemoveMember;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Removes another member. Owner only; cannot remove the last owner.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="PersonId">The member to remove.</param>
public sealed record RemoveMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId) : ICommand;
