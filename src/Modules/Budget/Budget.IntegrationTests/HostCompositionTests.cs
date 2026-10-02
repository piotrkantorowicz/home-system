namespace Budget.IntegrationTests;

using Budget.IntegrationTests.Infrastructure;
using global::Budget.Domain.Abstractions;
using global::Budget.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>Proves Budget composes into the real host alongside the other modules without stealing their unit of work.</summary>
public sealed class HostCompositionTests : IClassFixture<BudgetDatabaseFixture>, IDisposable
{
    private readonly BudgetApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared databases for this class.</param>
    public HostCompositionTests(BudgetDatabaseFixture fixture) => _factory = new BudgetApiFactory(fixture);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>The module-scoped unit of work is Budget's own context; the global one is not rebound to it.</summary>
    [Fact]
    public void BudgetUnitOfWork_IsBudgetsOwnContext_AndGlobalUnitOfWorkIsNot()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IBudgetUnitOfWork>().ShouldBeOfType<BudgetDbContext>();
        sp.GetRequiredService<IUnitOfWork>().ShouldNotBeOfType<BudgetDbContext>();
        sp.GetRequiredService<ICommandDispatcher>().ShouldNotBeNull();
        sp.GetRequiredService<IQueryDispatcher>().ShouldNotBeNull();
    }
}
