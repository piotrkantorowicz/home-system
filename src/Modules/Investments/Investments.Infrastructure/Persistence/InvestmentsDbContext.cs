namespace Investments.Infrastructure.Persistence;

using Investments.Application.Persistence;
using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.Fx;
using Microsoft.EntityFrameworkCore;

internal sealed class InvestmentsDbContext : DbContext, IInvestmentsUnitOfWork, IInvestmentsReadDbContext
{
    public InvestmentsDbContext(DbContextOptions<InvestmentsDbContext> options) : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<BalanceSnapshot> BalanceSnapshots => Set<BalanceSnapshot>();
    public DbSet<FxRate> FxRates => Set<FxRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestmentsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
