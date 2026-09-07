using Household.Application.Queries.Projections;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Queries.ListHouseholdMembers;

public sealed record ListHouseholdMembersQuery(string AuthSubject, Guid HouseholdId)
    : IQuery<IReadOnlyList<HouseholdMemberDto>>;
