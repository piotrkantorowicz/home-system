namespace Household.Infrastructure.Persistence;

using Household.Application.Persistence;
using Household.Domain.Abstractions;
using Household.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using HouseholdAggregate = Household.Domain.Aggregates.Household;

internal sealed class HouseholdDbContext : DbContext, IHouseholdUnitOfWork, IHouseholdReadDbContext
{
    public HouseholdDbContext(DbContextOptions<HouseholdDbContext> options) : base(options) { }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<HouseholdAggregate> Households => Set<HouseholdAggregate>();
    public DbSet<HouseholdInvitation> HouseholdInvitations => Set<HouseholdInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HouseholdDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
