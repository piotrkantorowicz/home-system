namespace Household.Api;

using Household.Api.Identity;
using Household.Infrastructure;
using Microsoft.AspNetCore.Authentication;
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

        // Enriches every authenticated principal with person_id / household_id / household_role.
        services.AddScoped<IClaimsTransformation, HouseholdClaimsTransformation>();

        return services;
    }

    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHouseholdEndpointsGroup();
        app.MapPersonEndpoints();
        return app;
    }
}
