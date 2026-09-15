namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Hydration</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class HydrationEndpointsTests
{
    private readonly HttpClient _client;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public HydrationEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    /// <summary><c>GET</c> hydration config returns ok.</summary>
    [Fact]
    public async Task GET_HydrationConfig_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/hydration/config");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>With valid request: <c>PUT</c> hydration config returns 204.</summary>
    [Fact]
    public async Task PUT_HydrationConfig_WithValidRequest_Returns204()
    {
        var request = new UpdateHydrationConfigRequest(2500, 250, true);

        var response = await _client.PutAsJsonAsync("/api/v1/hydration/config", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary>With target below minimum: <c>PUT</c> hydration config returns 400.</summary>
    [Fact]
    public async Task PUT_HydrationConfig_WithTargetBelowMinimum_Returns400()
    {
        var request = new UpdateHydrationConfigRequest(50, 250, true);

        var response = await _client.PutAsJsonAsync("/api/v1/hydration/config", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>With glass size below minimum: <c>PUT</c> hydration config returns 400.</summary>
    [Fact]
    public async Task PUT_HydrationConfig_WithGlassSizeBelowMinimum_Returns400()
    {
        var request = new UpdateHydrationConfigRequest(2500, 10, true);

        var response = await _client.PutAsJsonAsync("/api/v1/hydration/config", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>GET</c> water intake returns ok.</summary>
    [Fact]
    public async Task GET_WaterIntake_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/hydration/intake?date=2024-01-15");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>With valid request: <c>POST</c> water intake returns 201.</summary>
    [Fact]
    public async Task POST_WaterIntake_WithValidRequest_Returns201()
    {
        var request = new LogWaterIntakeRequest(TestClock.Today, 250, null);

        var response = await _client.PostAsJsonAsync("/api/v1/hydration/intake", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>With zero amount: <c>POST</c> water intake returns 400.</summary>
    [Fact]
    public async Task POST_WaterIntake_WithZeroAmount_Returns400()
    {
        var request = new LogWaterIntakeRequest(TestClock.Today, 0, null);

        var response = await _client.PostAsJsonAsync("/api/v1/hydration/intake", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>When entry does not exist: <c>DELETE</c> water intake returns 404.</summary>
    [Fact]
    public async Task DELETE_WaterIntake_WhenEntryDoesNotExist_Returns404()
    {
        var response = await _client.DeleteAsync($"/api/v1/hydration/intake/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary><c>PUT</c> then GET hydration config round trip.</summary>
    [Fact]
    public async Task PUT_ThenGET_HydrationConfigRoundTrip()
    {
        var request = new UpdateHydrationConfigRequest(3000, 300, false);
        var putResponse = await _client.PutAsJsonAsync("/api/v1/hydration/config", request);
        putResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/v1/hydration/config");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
