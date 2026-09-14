namespace Household.Application.Queries.GetCurrentPerson;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the person behind the caller's login; <see langword="null"/> before their first sync.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
public sealed record GetCurrentPersonQuery(string AuthSubject) : IQuery<CurrentPersonDto?>;
