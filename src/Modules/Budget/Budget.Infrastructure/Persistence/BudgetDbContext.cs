namespace Budget.Infrastructure.Persistence;

using Budget.Application.Persistence;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Ef.Inbox;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using BudgetAggregate = Budget.Domain.Aggregates.Budget;

internal sealed class BudgetDbContext : DbContext, IBudgetUnitOfWork, IBudgetReadDbContext
{
    public BudgetDbContext(DbContextOptions<BudgetDbContext> options) : base(options) { }

    public DbSet<BudgetAggregate> Budgets => Set<BudgetAggregate>();
    public DbSet<BudgetAccount> BudgetAccounts => Set<BudgetAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
