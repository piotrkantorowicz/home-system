namespace DietPlanner.Application.Persistence;

using DietPlanner.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public interface IDietPlannerReadDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<MealEntry> MealEntries { get; }
    DbSet<UserGoal> UserGoals { get; }
    DbSet<MealScheduleConfig> MealScheduleConfigs { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<NotificationPreferences> NotificationPreferences { get; }
    DbSet<HydrationConfig> HydrationConfigs { get; }
    DbSet<WaterIntake> WaterIntakes { get; }

    DatabaseFacade Database { get; }
}
