namespace Household.Infrastructure;

using Household.Application;
using Household.Application.Common;
using Household.Application.Persistence;
using Household.Contracts.Interfaces;
using Household.Domain.Abstractions;
using Household.Infrastructure.Persistence;
using Household.Infrastructure.Persistence.Repositories;
using Household.Infrastructure.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Wires the Household persistence, handlers and outbox. Called through <c>AddHouseholdModule</c>;
/// never directly by the host.
/// </summary>
public static partial class InfrastructureDependencyInjection
{
    /// <summary>Registers the module's <c>DbContext</c>, repositories, module-scoped unit of work, CQRS handlers and outbox.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the <c>Household</c> connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddHouseholdInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDomainEventDispatcher();

        services.AddDbContext<HouseholdDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Household"))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        services.AddOutbox<HouseholdDbContext>();

        // Module-scoped unit of work + read context — never the global IUnitOfWork
        // (owned by the first Style-1 module). See .claude/rules/backend-module-structure.md.
        services.AddScoped<IHouseholdUnitOfWork>(sp => sp.GetRequiredService<HouseholdDbContext>());
        services.AddScoped<IHouseholdReadDbContext>(sp => sp.GetRequiredService<HouseholdDbContext>());

        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IHouseholdInvitationRepository, HouseholdInvitationRepository>();
        services.AddScoped<HouseholdAccessService>();
        services.AddScoped<InvitationResolver>();

        // Read-side query surface for other modules + the host claims transformer.
        services.AddScoped<IHouseholdQueryService, HouseholdQueryService>();

        services.AddCqrsHandlers(AssemblyReference.Assembly);

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the Household database. Called by the host at startup in
    /// Development only; failures are logged, not thrown, so a missing database does not stop the host.
    /// </summary>
    /// <param name="serviceProvider">The built host provider; a scope is created from it.</param>
    /// <param name="logger">Receives the outcome.</param>
    public static async Task MigrateHouseholdDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HouseholdDbContext>();
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

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Household database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "An error occurred while applying Household database migrations")]
    private static partial void LogMigrationsFailed(ILogger logger, Exception exception);
}
