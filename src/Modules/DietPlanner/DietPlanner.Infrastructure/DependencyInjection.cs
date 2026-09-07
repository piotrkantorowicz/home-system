namespace DietPlanner.Infrastructure;

using DietPlanner.Application;
using DietPlanner.Application.Persistence;
using DietPlanner.Application.Workers;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.Services;
using DietPlanner.Infrastructure.Persistence;
using DietPlanner.Infrastructure.Persistence.Repositories;
using DietPlanner.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Persistence.Extensions;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddDietPlannerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDomainEventDispatcher();

        services.AddDbContext<DietPlannerDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("DietPlanner"))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddOutbox<DietPlannerDbContext>();

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IMealEntryRepository, MealEntryRepository>();
        services.AddScoped<IUserGoalRepository, UserGoalRepository>();
        services.AddScoped<IMealScheduleConfigRepository, MealScheduleConfigRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IDietReminderSettingsRepository, DietReminderSettingsRepository>();
        services.AddScoped<IHydrationConfigRepository, HydrationConfigRepository>();
        services.AddScoped<IWaterIntakeRepository, WaterIntakeRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DietPlannerDbContext>());
        services.AddScoped<IDietPlannerReadDbContext>(sp => sp.GetRequiredService<DietPlannerDbContext>());
        services.AddScoped<INutritionCalculator, NutritionCalculator>();
        services.AddScoped<WeightPredictionService>();

        services.AddCqrsHandlers(AssemblyReference.Assembly);

        // Meal-reminder ledger + read-side
        services.AddScoped<ISentMealReminderRepository, SentMealReminderRepository>();
        services.AddScoped<IMealReminderCandidateQueries, MealReminderCandidateQueries>();

        // Water-reminder ledger + read-side
        services.AddScoped<IWaterReminderStateRepository, WaterReminderStateRepository>();
        services.AddScoped<IWaterReminderCandidateQueries, WaterReminderCandidateQueries>();

        // Weekly-summary ledger + read-side
        services.AddScoped<IWeeklySummaryStateRepository, WeeklySummaryStateRepository>();
        services.AddScoped<IWeeklySummaryCandidateQueries, WeeklySummaryCandidateQueries>();

        // Diet reminder jobs (registered as IDietReminderJob; resolved per tick)
        services.AddScoped<IDietReminderJob, MealReminderJob>();
        services.AddScoped<IDietReminderJob, WaterReminderJob>();
        services.AddScoped<IDietReminderJob, WeeklySummaryJob>();

        // Tick service options + hosted service
        services.AddOptions<DietReminderTickServiceOptions>()
            .BindConfiguration(DietReminderTickServiceOptions.SectionName);
        services.AddHostedService<DietReminderTickService>();

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
