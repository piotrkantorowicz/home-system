namespace Household.Domain.Abstractions;

using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default);

    Task<Person?> GetByAuthSubjectAsync(string authSubject, CancellationToken ct = default);

    Task<Person?> GetByEmailAsync(PersonEmail email, CancellationToken ct = default);

    Task AddAsync(Person person, CancellationToken ct = default);
}
