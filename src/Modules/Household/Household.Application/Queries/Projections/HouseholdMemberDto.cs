namespace Household.Application.Queries.Projections;

/// <summary>
/// One member of a household as shown in rosters.
/// </summary>
/// <param name="PersonId">Person identifier.</param>
/// <param name="DisplayName">Name shown across the app.</param>
/// <param name="AvatarUrl">Avatar URL, if any.</param>
/// <param name="Role">Role name.</param>
/// <param name="Nickname">Household-local nickname, if set.</param>
/// <param name="IsManaged">True for a managed member without a login.</param>
public sealed record HouseholdMemberDto(
    Guid PersonId,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    string? Nickname,
    bool IsManaged);
