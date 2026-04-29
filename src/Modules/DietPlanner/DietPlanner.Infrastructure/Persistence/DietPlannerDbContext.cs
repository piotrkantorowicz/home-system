namespace DietPlanner.Infrastructure.Persistence;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Ledgers;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Messaging.Ef.Outbox;

internal sealed class DietPlannerDbContext : DbContext, IUnitOfWork, IDietPlannerReadDbContext
{
    public DietPlannerDbContext(DbContextOptions<DietPlannerDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<MealEntry> MealEntries => Set<MealEntry>();
    public DbSet<MealSlot> MealSlots => Set<MealSlot>();
    public DbSet<UserGoal> UserGoals => Set<UserGoal>();
    public DbSet<MealScheduleConfig> MealScheduleConfigs => Set<MealScheduleConfig>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<DietReminderSettings> DietReminderSettings => Set<DietReminderSettings>();
    public DbSet<HydrationConfig> HydrationConfigs => Set<HydrationConfig>();
    public DbSet<WaterIntake> WaterIntakes => Set<WaterIntake>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<SentMealReminder> SentMealReminders => Set<SentMealReminder>();
    public DbSet<WaterReminderState> WaterReminderStates => Set<WaterReminderState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DietPlannerDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public async Task CommitAsync(CancellationToken ct = default)
        => await SaveChangesAsync(ct);
}
