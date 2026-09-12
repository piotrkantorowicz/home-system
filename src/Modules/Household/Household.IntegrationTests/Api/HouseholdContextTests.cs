namespace Household.IntegrationTests.Api;

using System.Net.Http.Json;
using Household.Contracts.Interfaces;
using Household.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging.Ef.Outbox;
using HouseholdDb = Household.Infrastructure.Persistence.HouseholdDbContext;

/// <summary>
/// #219 — proves the Contracts query surface resolves a caller's household and that the
/// membership domain events land in the module outbox for other modules to consume.
/// </summary>
public sealed class HouseholdContextTests : IClassFixture<HouseholdDatabaseFixture>
{
    private readonly HouseholdApiFactory _factory;

    public HouseholdContextTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    private async Task<(string Sub, Guid PersonId)> SignedInPersonAsync()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: $"{Guid.NewGuid():N}@x.com", name: "Owner");
        (await client.PostAsync("/api/persons/me/sync", null)).EnsureSuccessStatusCode();
        var me = await client.GetFromJsonAsync<PersonMe>("/api/persons/me");
        return (sub, me!.Id);
    }

    private sealed record PersonMe(Guid Id);

    private async Task<T> WithScopeAsync<T>(Func<IServiceProvider, Task<T>> act)
    {
        using var scope = _factory.Services.CreateScope();
        return await act(scope.ServiceProvider);
    }

    [Fact]
    public async Task GetHouseholdContextForUser_ReturnsNull_WhenTheCallerHasNoHousehold()
    {
        var (sub, _) = await SignedInPersonAsync();

        var context = await WithScopeAsync(sp =>
            sp.GetRequiredService<IHouseholdQueryService>().GetHouseholdContextForUserAsync(sub));

        context.ShouldBeNull();
    }

    [Fact]
    public async Task GetHouseholdContextForUser_ReturnsNull_ForAnUnknownSubject()
    {
        var context = await WithScopeAsync(sp =>
            sp.GetRequiredService<IHouseholdQueryService>()
              .GetHouseholdContextForUserAsync($"auth|{Guid.NewGuid():N}"));

        context.ShouldBeNull();
    }

    [Fact]
    public async Task GetHouseholdContextForUser_ResolvesTheHousehold_AndRole()
    {
        var (sub, personId) = await SignedInPersonAsync();
        var client = _factory.CreateClientFor(sub);
        var create = await client.PostAsJsonAsync("/api/households", new { name = "Context House" });
        create.EnsureSuccessStatusCode();

        var context = await WithScopeAsync(sp =>
            sp.GetRequiredService<IHouseholdQueryService>().GetHouseholdContextForUserAsync(sub));

        context.ShouldNotBeNull();
        context!.PersonId.ShouldBe(personId);
        context.Role.ShouldBe("Owner");
        context.HouseholdId.ShouldNotBe(Guid.Empty);
        context.Members.ShouldHaveSingleItem().PersonId.ShouldBe(personId);
    }

    [Fact]
    public async Task CreatingAHousehold_WritesTheMembershipEvents_ToTheOutbox()
    {
        var (sub, _) = await SignedInPersonAsync();
        var client = _factory.CreateClientFor(sub);
        (await client.PostAsJsonAsync("/api/households", new { name = "Outbox House" })).EnsureSuccessStatusCode();

        var eventTypes = await WithScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<HouseholdDb>();
            return await db.Set<OutboxMessageEntity>()
                .AsNoTracking()
                .Select(m => m.EventType)
                .ToListAsync();
        });

        eventTypes.ShouldContain(t => t.Contains("HouseholdCreatedIntegrationEvent"));
        eventTypes.ShouldContain(t => t.Contains("MemberJoinedHouseholdIntegrationEvent"));
    }
}
