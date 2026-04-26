namespace DietPlanner.Application.Persistence;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public interface IDietPlannerReadDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<MealEntry> MealEntries { get; }
    DbSet<MealSlot> MealSlots { get; }
    DbSet<UserGoal> UserGoals { get; }
    DbSet<MealScheduleConfig> MealScheduleConfigs { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<DietReminderSettings> DietReminderSettings { get; }
    DbSet<HydrationConfig> HydrationConfigs { get; }
    DbSet<WaterIntake> WaterIntakes { get; }
    DbSet<WeightEntry> WeightEntries { get; }

    DatabaseFacade Database { get; }
}
