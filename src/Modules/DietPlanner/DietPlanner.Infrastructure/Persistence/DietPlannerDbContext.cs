namespace DietPlanner.Infrastructure.Persistence;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Domain;

internal sealed class DietPlannerDbContext : DbContext, IUnitOfWork, IDietPlannerReadDbContext
{
    public DietPlannerDbContext(DbContextOptions<DietPlannerDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<MealScheduleConfig> MealScheduleConfigs => Set<MealScheduleConfig>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();
    public DbSet<HydrationConfig> HydrationConfigs => Set<HydrationConfig>();
    public DbSet<WaterIntake> WaterIntakes => Set<WaterIntake>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietPlannerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
