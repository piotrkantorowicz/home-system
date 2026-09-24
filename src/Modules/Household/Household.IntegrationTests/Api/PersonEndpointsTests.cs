namespace Household.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using Household.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Person</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
public sealed class PersonEndpointsTests : IClassFixture<HouseholdDatabaseFixture>, IDisposable
{
    private readonly HouseholdApiFactory _factory;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fixture">The shared fixture for this collection.</param>
    public PersonEndpointsTests(HouseholdDatabaseFixture fixture)
        => _factory = new HouseholdApiFactory(fixture.ConnectionString);

    /// <summary>Disposes the application factory created for this test instance.</summary>
    public void Dispose() => _factory.Dispose();

    /// <summary>For a new subject: <c>Sync</c> creates the person and get me returns it.</summary>
    [Fact]
    public async Task Sync_ForANewSubject_CreatesThePerson_AndGetMeReturnsIt()
    {
        var sub = $"auth|{Guid.NewGuid():N}";
        var client = _factory.CreateClientFor(sub, email: "New.User@Example.com", name: "New User");

        var sync = await client.PostAsync("/api/persons/me/sync", content: null, cancellationToken: TestContext.Current.CancellationToken);
        sync.StatusCode.ShouldBe(HttpStatusCode.OK);
        var synced = await sync.Content.ReadFromJsonAsync<SyncBody>(cancellationToken: TestContext.Current.CancellationToken);
        synced!.PersonId.ShouldNotBe(Guid.Empty);

        var me = await client.GetFromJsonAsync<MeBody>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken);
        me!.Id.ShouldBe(synced.PersonId);
        me.DisplayName.ShouldBe("New User");
        me.Email.ShouldBe("new.user@example.com");
        me.IsManaged.ShouldBeFalse();
    }

    /// <summary><c>Sync</c> is idempotent and refreshes the profile.</summary>
    [Fact]
    public async Task Sync_IsIdempotent_AndRefreshesTheProfile()
    {
        var sub = $"auth|{Guid.NewGuid():N}";

        var first = await _factory.CreateClientFor(sub, name: "First")
            .PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken);
        var firstId = (await first.Content.ReadFromJsonAsync<SyncBody>(cancellationToken: TestContext.Current.CancellationToken))!.PersonId;

        var second = await _factory.CreateClientFor(sub, name: "Renamed")
            .PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken);
        var secondId = (await second.Content.ReadFromJsonAsync<SyncBody>(cancellationToken: TestContext.Current.CancellationToken))!.PersonId;

        secondId.ShouldBe(firstId);

        var me = await _factory.CreateClientFor(sub).GetFromJsonAsync<MeBody>("/api/persons/me", cancellationToken: TestContext.Current.CancellationToken);
        me!.DisplayName.ShouldBe("Renamed");
    }

    /// <summary>Before any sync: <c>GetMe</c> returns 404.</summary>
    [Fact]
    public async Task GetMe_BeforeAnySync_Returns404()
    {
        var client = _factory.CreateClientFor($"auth|{Guid.NewGuid():N}");

        var response = await client.GetAsync("/api/persons/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>Without authentication: <c>Sync</c> returns 401.</summary>
    [Fact]
    public async Task Sync_WithoutAuthentication_Returns401()
    {
        var response = await _factory.CreateClient().PostAsync("/api/persons/me/sync", null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed record SyncBody(Guid PersonId);

    private sealed record MeBody(Guid Id, string DisplayName, string? Email, string? AvatarUrl, bool IsManaged);
}
