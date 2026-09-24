namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Goal</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class GoalEndpointsTests
{
    private readonly HttpClient _client;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public GoalEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    /// <summary><c>GET</c> goals returns ok.</summary>
    [Fact]
    public async Task GET_Goals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/goals", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>With valid request: <c>POST</c> goal returns 201.</summary>
    [Fact]
    public async Task POST_Goal_WithValidRequest_Returns201()
    {
        var request = new GoalRequest(2000, 150m, 250m, 70m, 30m);

        var response = await _client.PostAsJsonAsync("/api/v1/goals", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
