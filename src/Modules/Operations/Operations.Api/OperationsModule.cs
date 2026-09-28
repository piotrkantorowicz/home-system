namespace Operations.Api;

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Operations.Application.Outbox;
using Shared.Infrastructure.Cqrs.Extensions;

/// <summary>
/// The Operations module: admin tooling that spans the other modules' infrastructure (today the
/// outbox dead letters). It owns no data — it works through the stores each module registers — so
/// it has only Application and Api layers.
/// </summary>
public static class OperationsModule
{
    /// <summary>Registers the Operations handlers. Call before <c>AddCqrsDispatchers()</c>.</summary>
    /// <param name="services">The host service collection.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOperationsModule(this IServiceCollection services)
    {
        services.AddScoped<OutboxModules>();
        services.AddCqrsHandlers(typeof(OutboxModules).Assembly);
        return services;
    }

    /// <summary>Maps the Operations admin endpoints.</summary>
    /// <param name="app">The host route builder.</param>
    /// <returns><paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
        => app.MapOutboxAdminEndpoints();
}
