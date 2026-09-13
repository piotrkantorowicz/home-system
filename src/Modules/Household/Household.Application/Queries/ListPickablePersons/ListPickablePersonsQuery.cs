namespace Household.Application.Queries.ListPickablePersons;

using Shared.Abstractions.Cqrs;

/// <summary>People who can be added to a household: anyone not already in one.</summary>
public sealed record ListPickablePersonsQuery(string AuthSubject)
    : IQuery<IReadOnlyList<PickablePersonDto>>;

public sealed record PickablePersonDto(
    Guid PersonId,
    string DisplayName,
    string? Email,
    bool IsManaged);
