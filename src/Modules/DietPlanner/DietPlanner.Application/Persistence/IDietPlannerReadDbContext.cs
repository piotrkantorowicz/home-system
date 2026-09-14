namespace DietPlanner.Application.Persistence;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Read-side view of the Diet Planner <c>DbContext</c> for query handlers, which project straight
/// from these sets with <c>AsNoTracking()</c> + <c>Select()</c>. Keeps the Application project
/// free of the concrete context while still letting queries bypass repositories.
/// </summary>
public interface IDietPlannerReadDbContext
{
    /// <summary>Products; a global filter hides soft-deleted rows.</summary>
    DbSet<Product> Products { get; }
    /// <summary>Recipes; a global filter hides soft-deleted rows.</summary>
    DbSet<Recipe> Recipes { get; }
    /// <summary>Planned meals with their actual-product lines.</summary>
    DbSet<MealEntry> MealEntries { get; }
    /// <summary>Meal schedule slots, exposed for joins from meal entries.</summary>
    DbSet<MealSlot> MealSlots { get; }
    /// <summary>Per-user nutrition goals.</summary>
    DbSet<UserGoal> UserGoals { get; }
    /// <summary>Per-user meal schedules.</summary>
    DbSet<MealScheduleConfig> MealScheduleConfigs { get; }
    /// <summary>Per-user body profiles.</summary>
    DbSet<UserProfile> UserProfiles { get; }
    /// <summary>Per-user notification preferences.</summary>
    DbSet<DietReminderSettings> DietReminderSettings { get; }
    /// <summary>Per-user hydration preferences.</summary>
    DbSet<HydrationConfig> HydrationConfigs { get; }
    /// <summary>Logged drinks.</summary>
    DbSet<WaterIntake> WaterIntakes { get; }
    /// <summary>Weigh-ins.</summary>
    DbSet<WeightEntry> WeightEntries { get; }

    /// <summary>Access to the underlying connection, used by the purge command for set-based deletes.</summary>
    DatabaseFacade Database { get; }
}
