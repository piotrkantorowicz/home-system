namespace Investments.Infrastructure;

using Investments.Application;
using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.Abstractions;
using Investments.Infrastructure.Persistence;
using Investments.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Cqrs.Extensions;
using Shared.Infrastructure.Persistence.Extensions;

/// <summary>
/// Wires the Investments persistence and handlers. Called through <c>AddInvestmentsModule</c>;
/// never directly by the host.
/// </summary>
public static partial class InfrastructureDependencyInjection
{
    /// <summary>Registers the module's <c>DbContext</c>, repositories, module-scoped unit of work and CQRS handlers.</summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">Provides the <c>Investments</c> connection string.</param>
    /// <returns><paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddInvestmentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDomainEventDispatcher();

        services.AddDbContext<InvestmentsDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Investments"))
                .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

        // No outbox: Investments publishes no integration events (design §6).

        // Module-scoped unit of work + read context — never the global IUnitOfWork
        // (owned by the first Style-1 module). See docs/rules/backend-module-structure.md.
        services.AddScoped<IInvestmentsUnitOfWork>(sp => sp.GetRequiredService<InvestmentsDbContext>());
        services.AddScoped<IInvestmentsReadDbContext>(sp => sp.GetRequiredService<InvestmentsDbContext>());

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<InvestmentsAccessService>();

        services.AddCqrsHandlers(AssemblyReference.Assembly);

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations to the Investments database. Called by the host at startup in
    /// Development only; failures are logged, not thrown, so a missing database does not stop the host.
    /// </summary>
    /// <param name="serviceProvider">The built host provider; a scope is created from it.</param>
    /// <param name="logger">Receives the outcome.</param>
    /// <param name="ct">Cancels the migration; cancellation propagates instead of being logged as a failure.</param>
    public static async Task MigrateInvestmentsDatabaseAsync(
        this IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync(ct);
            LogMigrationsApplied(logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogMigrationsFailed(logger, ex);
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Investments database migrations applied successfully")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "An error occurred while applying Investments database migrations")]
    private static partial void LogMigrationsFailed(ILogger logger, Exception exception);
}
