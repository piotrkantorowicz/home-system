namespace DietPlanner.Api;

using DietPlanner.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The Diet Planner module's public registration surface — the only two calls the host makes into
/// the module.
/// </summary>
public static class DietPlannerModule
{
    /// <summary>Registers every Diet Planner service. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the module's connection string and options.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddDietPlannerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDietPlannerInfrastructure(configuration);
        return services;
    }

    /// <summary>Maps every Diet Planner endpoint group under <c>/api/v1</c>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapDietPlannerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapProductEndpoints();
        app.MapRecipeEndpoints();
        app.MapMealEndpoints();
        app.MapGoalEndpoints();
        app.MapMealScheduleEndpoints();
        app.MapProfileEndpoints();
        app.MapDietReminderSettingsEndpoints();
        app.MapHydrationEndpoints();
        app.MapWeightEntryEndpoints();
        app.MapWeeklySummaryEndpoints();

        if (IsTestSupportEnabled(app))
            app.MapTestSupportEndpoints();

        return app;
    }

    private static bool IsTestSupportEnabled(IEndpointRouteBuilder app)
    {
        var services = app.ServiceProvider;
        var env = services.GetRequiredService<IHostEnvironment>();
        var config = services.GetRequiredService<IConfiguration>();

        return env.IsDevelopment()
            || config.GetValue<bool>("E2ETestSupport:Enabled");
    }
}
