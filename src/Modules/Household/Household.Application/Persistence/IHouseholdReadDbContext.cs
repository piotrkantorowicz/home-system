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
    /// <summary>Every person, linked or managed.</summary>
    DbSet<Person> Persons { get; }

    /// <summary>Households with their members.</summary>
    DbSet<HouseholdAggregate> Households { get; }

    /// <summary>Invitations in every status.</summary>
    DbSet<HouseholdInvitation> HouseholdInvitations { get; }
}
