namespace Budget.Api;

using Budget.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The Budget module's public registration surface — the only two calls the host makes into the module.</summary>
public static class BudgetModule
{
    /// <summary>Registers every Budget service. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the module's connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddBudgetModule(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddBudgetInfrastructure(configuration);

    /// <summary>Maps the Budget endpoint group under <c>/api/budget</c>.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
        => app.MapBudgetEndpointsGroup();
}
