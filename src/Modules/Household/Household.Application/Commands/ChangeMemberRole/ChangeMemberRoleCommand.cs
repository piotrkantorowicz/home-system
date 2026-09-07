using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    Guid PersonId,
    HouseholdRole Role) : ICommand;
