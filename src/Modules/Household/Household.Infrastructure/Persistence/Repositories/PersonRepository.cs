namespace Household.Infrastructure.Persistence.Repositories;

using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Household.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class PersonRepository : IPersonRepository
{
    private readonly HouseholdDbContext _dbContext;

    public PersonRepository(HouseholdDbContext dbContext) => _dbContext = dbContext;

    public Task<Person?> GetByIdAsync(PersonId id, CancellationToken ct = default)
        => _dbContext.Persons.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Person?> GetByAuthSubjectAsync(string authSubject, CancellationToken ct = default)
        => _dbContext.Persons.FirstOrDefaultAsync(p => p.AuthSubject == authSubject, ct);

    public Task<Person?> GetByEmailAsync(PersonEmail email, CancellationToken ct = default)
        => _dbContext.Persons.FirstOrDefaultAsync(p => p.Email != null && p.Email.Value == email.Value, ct);

    public async Task AddAsync(Person person, CancellationToken ct = default)
        => await _dbContext.Persons.AddAsync(person, ct);
}
