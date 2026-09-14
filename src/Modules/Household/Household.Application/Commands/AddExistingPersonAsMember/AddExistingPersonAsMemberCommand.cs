namespace Household.Application.Commands.AddExistingPersonAsMember;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Adds a person who already exists (linked or managed) and belongs to no household. Owner only.
/// </summary>
/// <param name="RequestingAuthSubject">Auth subject of the caller; must be an owner of the household or the command fails with a forbidden error.</param>
/// <param name="HouseholdId">The household acted on.</param>
/// <param name="PersonId">The person to add; must not be in any household.</param>
/// <param name="Role">Their role in this household.</param>
/// <param name="Nickname">Optional household-local nickname.</param>
public sealed record AddExistingPersonAsMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role,
    string? Nickname) : ICommand;
