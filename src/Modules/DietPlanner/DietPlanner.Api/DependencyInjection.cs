namespace DietPlanner.Api;

using DietPlanner.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class DietPlannerModule
{
    public static IServiceCollection AddDietPlannerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDietPlannerInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder MapDietPlannerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapProductEndpoints();
        app.MapRecipeEndpoints();
        app.MapMealEndpoints();
        app.MapGoalEndpoints();
        app.MapMealScheduleEndpoints();
        app.MapProfileEndpoints();
        app.MapNotificationPreferencesEndpoints();
        app.MapHydrationEndpoints();
        app.MapWeightEntryEndpoints();

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
