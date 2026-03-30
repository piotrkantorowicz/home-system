namespace DietPlanner.Application.Persistence;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IDietPlannerReadDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Recipe> Recipes { get; }
    DbSet<MealEntry> MealEntries { get; }
    DbSet<UserGoal> UserGoals { get; }
}
