namespace Household.Domain.Abstractions;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;

/// <summary>
/// Write-side access to <see cref="Person"/> aggregates. Persons are never deleted. Queries bypass
/// this and read the DbContext directly.
/// </summary>
public interface IPersonRepository
{
    /// <summary>Loads a person by identifier for mutation.</summary>
    /// <param name="id">The identifier.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked person, or <see langword="null"/> when it does not exist.</returns>
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default);

    /// <summary>Finds the person linked to an Authentik account — the lookup every sign-in starts with.</summary>
    /// <param name="authSubject">The subject claim.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked person, or <see langword="null"/> on a first sign-in.</returns>
    Task<Person?> GetByAuthSubjectAsync(string authSubject, CancellationToken ct = default);

    /// <summary>Finds the person recorded under an email, used to resolve managed-member links and invitations at sign-in.</summary>
    /// <param name="email">The normalised address.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    /// <returns>The tracked person, or <see langword="null"/>.</returns>
    Task<Person?> GetByEmailAsync(PersonEmail email, CancellationToken ct = default);

    /// <summary>Stages a new person; it is written when the unit of work commits.</summary>
    /// <param name="person">The person to add.</param>
    /// <param name="ct">Propagates cancellation to the storage call.</param>
    Task AddAsync(Person person, CancellationToken ct = default);
}
