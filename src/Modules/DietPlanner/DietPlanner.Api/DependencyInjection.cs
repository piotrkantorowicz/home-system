namespace DietPlanner.Api;

using DietPlanner.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return app;
    }
}
