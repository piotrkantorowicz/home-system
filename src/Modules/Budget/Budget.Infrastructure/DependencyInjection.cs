namespace Budget.Infrastructure;

using Budget.Application;
using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.Abstractions;
using Budget.Infrastructure.Persistence;
using Budget.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Messaging.Ef.Extensions;
using Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Wires the Budget persistence, handlers and outbox. Called through <c>AddBudgetModule</c>;
/// never directly by the host.
/// </summary>
public static partial class InfrastructureDependencyInjection
{
    /// <summary>Registers the module's <c>DbContext</c>, repositories, module-scoped unit of work, CQRS handlers and outbox.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the <c>Budget</c> connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddBudgetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDomainEventDispatcher();

        services.AddDbContext<BudgetDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Budget"))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        // Registered per repository convention; Budget publishes no integration events yet.
        services.AddOutbox<BudgetDbContext>();

        // Module-scoped unit of work + read context — never the global IUnitOfWork
        // (owned by the first Style-1 module). See docs/rules/backend-module-structure.md.
        services.AddScoped<IBudgetUnitOfWork>(sp => sp.GetRequiredService<BudgetDbContext>());
        services.AddScoped<IBudgetReadDbContext>(sp => sp.GetRequiredService<BudgetDbContext>());

        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IBudgetAccountRepository, BudgetAccountRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<BudgetAccessService>();

        services.AddCqrsHandlers(AssemblyReference.Assembly);

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the Budget database. Called by the host at startup in
    /// Development only; failures are logged, not thrown, so a missing database does not stop the host.
    /// </summary>
    /// <param name="serviceProvider">The built host provider; a scope is created from it.</param>
    /// <param name="logger">Receives the outcome.</param>
    public static async Task MigrateBudgetDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetDbContext>();
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

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Budget database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "An error occurred while applying Budget database migrations")]
    private static partial void LogMigrationsFailed(ILogger logger, Exception exception);
}
