using Shared.Abstractions.Cqrs;

namespace Household.Application.Queries.ListPendingInvitations;

public sealed record ListPendingInvitationsQuery(string AuthSubject, Guid HouseholdId)
    : IQuery<IReadOnlyList<InvitationDto>>;

public sealed record InvitationDto(
    Guid Id,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt);
