namespace Household.Application.Queries.ListHouseholdMembers;

using Household.Application.Queries.Projections;
using Shared.Abstractions.Cqrs;

public sealed record ListHouseholdMembersQuery(string AuthSubject, Guid HouseholdId)
    : IQuery<IReadOnlyList<HouseholdMemberDto>>;
