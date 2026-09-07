using Household.Application.Queries.Projections;
using Shared.Abstractions.Cqrs;

namespace Household.Application.Queries.GetMyHousehold;

public sealed record GetMyHouseholdQuery(string AuthSubject) : IQuery<MyHouseholdDto?>;

public sealed record MyHouseholdDto(
    Guid Id,
    string Name,
    string MyRole,
    IReadOnlyList<HouseholdMemberDto> Members);
