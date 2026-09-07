namespace Household.Api;

using Household.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class HouseholdModule
{
    public static IServiceCollection AddHouseholdModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHouseholdInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHouseholdEndpointsGroup();
        app.MapPersonEndpoints();
        return app;
    }
}
