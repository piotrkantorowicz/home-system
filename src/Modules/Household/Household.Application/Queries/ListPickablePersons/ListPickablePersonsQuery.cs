using Shared.Abstractions.Cqrs;

namespace Household.Application.Queries.ListPickablePersons;

/// <summary>People who can be added to a household: anyone not already in one.</summary>
public sealed record ListPickablePersonsQuery(string AuthSubject)
    : IQuery<IReadOnlyList<PickablePersonDto>>;

public sealed record PickablePersonDto(
    Guid PersonId,
    string DisplayName,
    string? Email,
    bool IsManaged);
