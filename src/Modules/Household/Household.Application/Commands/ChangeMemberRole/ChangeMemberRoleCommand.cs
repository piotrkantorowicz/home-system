namespace Household.Application.Commands.ChangeMemberRole;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Changes a member's role. Owner only; cannot demote the last owner.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="PersonId">The member whose role changes.</param>
/// <param name="Role">The new role.</param>
public sealed record ChangeMemberRoleCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role) : ICommand;
