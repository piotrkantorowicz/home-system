namespace Household.IntegrationTests;

using global::Household.Contracts.Events;
using global::Household.Contracts.Interfaces;
using global::Household.Domain.Abstractions;
using global::Household.Infrastructure.Persistence;
using Household.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Serialization;

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

    /// <summary>The host serializer accepts Household events, so the outbox can dispatch them (#416).</summary>
    [Fact]
    public void IntegrationEventSerializer_HouseholdEvent_RoundTrips()
    {
        var serializer = _factory.Services.GetRequiredService<IIntegrationEventSerializer>();
        var @event = new HouseholdCreatedIntegrationEvent(
            Guid.CreateVersion7(), new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc), Guid.CreateVersion7(), Guid.CreateVersion7());

        // A non-generic, non-open type always has an assembly-qualified name.
        var result = serializer.Deserialize(
            serializer.Serialize(@event), typeof(HouseholdCreatedIntegrationEvent).AssemblyQualifiedName!);

        result.ShouldBe(@event);
    }
}
