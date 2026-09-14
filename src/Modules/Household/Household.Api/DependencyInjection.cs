namespace Household.Api;

using Household.Api.Identity;
using Household.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The Household module's public registration surface — the only two calls the host makes into the
/// module. Also registers the claims transformation that stamps household context onto every
/// authenticated principal.
/// </summary>
public static class HouseholdModule
{
    /// <summary>Registers every Household service. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the module's connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddHouseholdModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHouseholdInfrastructure(configuration);

        // Enriches every authenticated principal with person_id / household_id / household_role.
        services.AddScoped<IClaimsTransformation, HouseholdClaimsTransformation>();

        return services;
    }

    /// <summary>Maps the person and household endpoint groups under <c>/api</c>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHouseholdEndpointsGroup();
        app.MapPersonEndpoints();
        return app;
    }
}
