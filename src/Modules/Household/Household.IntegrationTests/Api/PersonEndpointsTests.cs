namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

public sealed class PersonEndpointsTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    public PersonEndpointsTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Sync_ForANewSubject_CreatesThePerson_AndGetMeReturnsIt()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: "New.User@Example.com", name: "New User");

        var sync = await client.PostAsync("/api/persons/me/sync", content: null);
        sync.StatusCode.ShouldBe(HttpStatusCode.OK);
        var synced = await sync.Content.ReadFromJsonAsync<SyncBody>();
        synced!.PersonId.ShouldNotBe(Guid.Empty);

        var me = await client.GetFromJsonAsync<MeBody>("/api/persons/me");
        me!.Id.ShouldBe(synced.PersonId);
        me.DisplayName.ShouldBe("New User");
        me.Email.ShouldBe("new.user@example.com");
        me.IsManaged.ShouldBeFalse();
    }

    [Fact]
    public async Task Sync_IsIdempotent_AndRefreshesTheProfile()
    {
        var sub = $"auth|{Guid.NewGuid():N}";

        var first = await _factory.CreateClientFor(sub, name: "First")
            .PostAsync("/api/persons/me/sync", null);
        var firstId = (await first.Content.ReadFromJsonAsync<SyncBody>())!.PersonId;

        var second = await _factory.CreateClientFor(sub, name: "Renamed")
            .PostAsync("/api/persons/me/sync", null);
        var secondId = (await second.Content.ReadFromJsonAsync<SyncBody>())!.PersonId;

        secondId.ShouldBe(firstId);

        var me = await _factory.CreateClientFor(sub).GetFromJsonAsync<MeBody>("/api/persons/me");
        me!.DisplayName.ShouldBe("Renamed");
    }

    [Fact]
    public async Task GetMe_BeforeAnySync_Returns404()
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}");

        var response = await client.GetAsync("/api/persons/me");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sync_WithoutAuthentication_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/persons/me/sync", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record SyncBody(Guid PersonId);

    private sealed record MeBody(Guid Id, string DisplayName, string? Email, string? AvatarUrl, bool IsManaged);
}
