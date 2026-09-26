namespace DietPlanner.Infrastructure;

using DietPlanner.Application;
using DietPlanner.Application.Households;
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

/// <summary>
/// Wires the Diet Planner persistence, handlers, outbox, integration-event consumers and background
/// jobs. Called through <c>AddDietPlannerModule</c>; never directly by the host.
/// </summary>
public static partial class InfrastructureDependencyInjection
{
    /// <summary>Registers the module's <c>DbContext</c>, repositories, unit of work, CQRS handlers, outbox/inbox and reminder jobs.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the <c>DietPlanner</c> connection string and worker options.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
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
        services.AddScoped<HouseholdRosterProvider>();

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
            .BindConfiguration(DietReminderTickServiceOptions.SectionName)
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZoneId, out _), "TimeZoneId is not a known time zone.")
            .ValidateOnStart();
        services.AddHostedService<DietReminderTickService>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the Diet Planner database. Called by the host at startup in
    /// Development only; failures are logged, not thrown, so a missing database does not stop the host.
    /// </summary>
    /// <param name="serviceProvider">The built host provider; a scope is created from it.</param>
    /// <param name="logger">Receives the outcome.</param>
    public static async Task MigrateDietPlannerDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            LogMigrationsApplied(logger);
        }
        catch (Exception ex)
        {
            LogMigrationsFailed(logger, ex);
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "DietPlanner database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "An error occurred while applying DietPlanner database migrations")]
    private static partial void LogMigrationsFailed(ILogger logger, Exception exception);
}
