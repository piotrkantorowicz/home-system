namespace Household.Application.Queries.ListPickablePersons;

using Shared.Abstractions.Cqrs;

/// <summary>People who can be added to a household: anyone not already in one.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record ListPickablePersonsQuery(string AuthSubject)
    : IQuery<IReadOnlyList<PickablePersonDto>>;

/// <summary>
/// A person who can be added to a household.
/// </summary>
/// <param name="PersonId">Person identifier.</param>
/// <param name="DisplayName">Name shown across the app.</param>
/// <param name="Email">Email, if known.</param>
/// <param name="IsManaged">True for a managed person without a login.</param>
public sealed record PickablePersonDto(
    Guid PersonId,
    string DisplayName,
    string? Email,
    bool IsManaged);
