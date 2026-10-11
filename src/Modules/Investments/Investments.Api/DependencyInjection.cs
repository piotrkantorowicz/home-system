namespace Investments.Api;

using Investments.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The Investments module's public registration surface — the only two calls the host makes into the module.</summary>
public static class InvestmentsModule
{
    /// <summary>Registers every Investments service. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the module's connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddInvestmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddInvestmentsInfrastructure(configuration);

    /// <summary>Maps the Investments endpoint group under <c>/api/investments</c>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapInvestmentsEndpoints(this IEndpointRouteBuilder app)
        => app.MapInvestmentsEndpointsGroup();
}
