namespace Household.IntegrationTests;

using global::Household.Contracts.Interfaces;
using global::Household.Domain.Abstractions;
using global::Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Proves the Household module composes into the real host without breaking DI —
/// the scaffold acceptance for #214. Endpoint + auth coverage arrives with #217 / #226.
/// </summary>
public sealed class HostCompositionTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public HostCompositionTests(HouseholdDatabaseFixture fixture)
        => _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Household", fixture.ConnectionString);
        });

    /// <summary><c>HouseholdModule</c> resolves its scoped services and from the host.</summary>
    [Fact]
    public void HouseholdModule_ResolvesItsScopedServices_FromTheHost()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetService<HouseholdDbContext>().ShouldNotBeNull();
        sp.GetService<IHouseholdUnitOfWork>().ShouldNotBeNull();
        sp.GetService<ICommandDispatcher>().ShouldNotBeNull();
        sp.GetService<IQueryDispatcher>().ShouldNotBeNull();
        sp.GetService<IHouseholdQueryService>().ShouldNotBeNull();
        sp.GetServices<IClaimsTransformation>()
            .ShouldContain(t => t.GetType().Name == "HouseholdClaimsTransformation");
    }
}
