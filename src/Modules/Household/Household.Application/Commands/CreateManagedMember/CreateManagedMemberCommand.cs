namespace Household.Application.Commands.CreateManagedMember;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates a managed person (no login, e.g. a child) and adds them to the household in one step; returns the new person's id. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="DisplayName">Name shown across the app; required.</param>
/// <param name="Email">Optional address a future login can be matched against.</param>
/// <param name="Role">Their role in this household.</param>
/// <param name="Nickname">Optional household-local nickname.</param>
public sealed record CreateManagedMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string DisplayName,
    string? Email,
    HouseholdRole Role,
    string? Nickname) : ICommand<Guid>;
