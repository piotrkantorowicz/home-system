using Household.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.CreateManagedMember;

public sealed record CreateManagedMemberCommand(
    string RequestingAuthSubject,
    Guid HouseholdId,
    string DisplayName,
    string? Email,
    HouseholdRole Role,
    string? Nickname) : ICommand<Guid>;
