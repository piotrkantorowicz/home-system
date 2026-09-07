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

public static class InfrastructureDependencyInjection
{
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
        services.AddScoped<HouseholdAccessService>();

        // Read-side query surface for other modules + the host claims transformer.
        services.AddScoped<IHouseholdQueryService, HouseholdQueryService>();

        services.AddCqrsHandlers(AssemblyReference.Assembly);

        return services;
    }

    public static async Task MigrateHouseholdDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HouseholdDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Household database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying Household database migrations");
        }
    }
}
