namespace Household.Application.Queries.Projections;

public sealed record HouseholdMemberDto(
    Guid PersonId,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string? Nickname,
    bool IsManaged);
