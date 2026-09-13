namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.Application.Queries.GetWeightEntries;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollectionDefinition.Name)]
public sealed class WeightEntryEndpointsTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly DatabaseFixture _db;

    public WeightEntryEndpointsTests(DatabaseFixture db) => _db = db;

    private HttpClient CreateClient(string userId)
        => new DietPlannerWebApplicationFactory(_db.ConnectionString, userId).CreateClient();

    private static async Task EnsureProfileExists(HttpClient client)
    {
        var existing = await client.GetAsync("/api/v1/profile");
        if (existing.StatusCode == HttpStatusCode.OK) return;

        var request = new ProfileRequest(
            new DateOnly(1990, 1, 1), "Male", 180m, 80m, 75m, "ModeratelyActive");
        var response = await client.PostAsJsonAsync("/api/v1/profile", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_WeightEntry_WithValidRequest_Returns201()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries",
            new LogWeightEntryRequest(Today, 78.5m));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<LogWeightEntryResponse>();
        body.ShouldNotBeNull();
        body.Created.ShouldBeTrue();
    }

    [Fact]
    public async Task POST_WeightEntry_SameDateTwice_UpdatesExisting()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var first = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 80m));
        first.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<LogWeightEntryResponse>();

        var second = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 79m));
        second.StatusCode.ShouldBe(HttpStatusCode.Created);
        var secondBody = await second.Content.ReadFromJsonAsync<LogWeightEntryResponse>();

        secondBody.ShouldNotBeNull();
        secondBody.Created.ShouldBeFalse();
        secondBody.Id.ShouldBe(firstBody!.Id);
    }

    [Fact]
    public async Task POST_WeightEntry_UpdatesProfileCurrentWeight()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 72.5m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var profileResponse = await client.GetAsync("/api/v1/profile");
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.CurrentWeightKg.ShouldBe(72.5m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000)]
    public async Task POST_WeightEntry_WithInvalidWeight_Returns400(decimal weight)
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, weight));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_WeightEntry_WithFutureDate_Returns400()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today.AddDays(1), 80m));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_WeightEntries_ReturnsOrderedByDateAscending()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today.AddDays(-2), 81m));
        await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 80m));
        await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today.AddDays(-1), 80.5m));

        var response = await client.GetAsync("/api/v1/weight-entries");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var entries = await response.Content.ReadFromJsonAsync<IReadOnlyList<WeightEntryDto>>();
        entries.ShouldNotBeNull();
        entries.Count.ShouldBe(3);
        entries[0].Date.ShouldBe(Today.AddDays(-2));
        entries[1].Date.ShouldBe(Today.AddDays(-1));
        entries[2].Date.ShouldBe(Today);
    }

    [Fact]
    public async Task GET_WeightEntries_WithFromGreaterThanTo_Returns400()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.GetAsync(
            $"/api/v1/weight-entries?from={Today:yyyy-MM-dd}&to={Today.AddDays(-5):yyyy-MM-dd}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_WeightEntry_Returns204AndRecomputesProfileCurrentWeight()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var olderResponse = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today.AddDays(-1), 81m));
        var latestResponse = await client.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 80m));
        var latestBody = await latestResponse.Content.ReadFromJsonAsync<LogWeightEntryResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/weight-entries/{latestBody!.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/v1/profile");
        profile!.CurrentWeightKg.ShouldBe(81m);
    }

    [Fact]
    public async Task DELETE_WeightEntry_NonExistent_Returns404()
    {
        var client = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(client);

        var response = await client.DeleteAsync($"/api/v1/weight-entries/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_WeightEntry_OtherUserEntry_Returns404()
    {
        var owner = $"weight-user-{Guid.NewGuid():N}";
        var ownerClient = CreateClient(owner);
        await EnsureProfileExists(ownerClient);

        var created = await ownerClient.PostAsJsonAsync(
            "/api/v1/weight-entries", new LogWeightEntryRequest(Today, 80m));
        var body = await created.Content.ReadFromJsonAsync<LogWeightEntryResponse>();

        var otherClient = CreateClient($"weight-user-{Guid.NewGuid():N}");
        await EnsureProfileExists(otherClient);

        var response = await otherClient.DeleteAsync($"/api/v1/weight-entries/{body!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
