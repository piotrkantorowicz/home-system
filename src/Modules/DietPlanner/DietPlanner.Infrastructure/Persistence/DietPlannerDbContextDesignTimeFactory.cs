namespace DietPlanner.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

internal sealed class DietPlannerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<DietPlannerDbContext>
{
    public DietPlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DietPlannerDbContext>()
            .UseNpgsql("Host=localhost;Database=design-time;Username=design;Password=design")
            .Options;

        return new DietPlannerDbContext(options);
    }
}
