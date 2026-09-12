namespace Household.Application.Persistence;

using Household.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

/// <summary>
/// Read-side surface over the module's <c>DbContext</c>. Query handlers depend on this and
/// project with <c>AsNoTracking()</c> + <c>Select()</c> — they never load aggregates.
/// </summary>
public interface IHouseholdReadDbContext
{
    DbSet<Person> Persons { get; }

    DbSet<HouseholdAggregate> Households { get; }

    DbSet<HouseholdInvitation> HouseholdInvitations { get; }
}
