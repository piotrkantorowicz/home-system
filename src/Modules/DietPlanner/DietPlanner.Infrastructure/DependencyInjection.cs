namespace DietPlanner.Infrastructure;

using DietPlanner.Application;
using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.Services;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Extensions;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddDietPlannerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DietPlannerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DietPlanner")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealEntryRepository, MealEntryRepository>();
        services.AddScoped<IUserGoalRepository, UserGoalRepository>();
        services.AddScoped<IMealScheduleConfigRepository, MealScheduleConfigRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<INotificationPreferencesRepository, NotificationPreferencesRepository>();
        services.AddScoped<IHydrationConfigRepository, HydrationConfigRepository>();
        services.AddScoped<IWaterIntakeRepository, WaterIntakeRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DietPlannerDbContext>());
        services.AddScoped<IDietPlannerReadDbContext>(sp => sp.GetRequiredService<DietPlannerDbContext>());
        services.AddScoped<INutritionCalculator, NutritionCalculator>();
        services.AddScoped<WeightPredictionService>();

        services.AddCqrs<DietPlannerDbContext>(AssemblyReference.Assembly);

        return services;
    }

    public static async Task MigrateDietPlannerDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("DietPlanner database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying DietPlanner database migrations");
        }
    }
}
