namespace Household.Application.Queries.ListHouseholdMembers;

using Household.Application.Queries.Projections;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists the members of a household the caller belongs to.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller; must be a member.</param>
/// <param name="HouseholdId">The household acted on.</param>
public sealed record ListHouseholdMembersQuery(string AuthSubject, Guid HouseholdId)
    : IQuery<IReadOnlyList<HouseholdMemberDto>>;
