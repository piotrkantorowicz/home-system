namespace Household.Application.Commands.ChangeMemberRole;

using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

public sealed record ChangeMemberRoleCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role) : ICommand;
