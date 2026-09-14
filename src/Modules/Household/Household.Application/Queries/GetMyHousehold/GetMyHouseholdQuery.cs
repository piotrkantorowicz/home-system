namespace Household.Application.Queries.GetMyHousehold;

using Household.Application.Queries.Projections;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the household the caller belongs to with its roster; <see langword="null"/> when they are in none.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record GetMyHouseholdQuery(string AuthSubject) : IQuery<MyHouseholdDto?>;

/// <summary>
/// The caller's household as seen by them.
/// </summary>
/// <param name="Id">Household identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="MyRole">The caller's role name.</param>
/// <param name="Members">Every member, including the caller.</param>
public sealed record MyHouseholdDto(
    Guid Id,
    string Name,
    string MyRole,
    IReadOnlyList<HouseholdMemberDto> Members);
