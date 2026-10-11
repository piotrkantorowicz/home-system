namespace Investments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

internal sealed class InvestmentsDbContext : DbContext
{
    public InvestmentsDbContext(DbContextOptions<InvestmentsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvestmentsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
