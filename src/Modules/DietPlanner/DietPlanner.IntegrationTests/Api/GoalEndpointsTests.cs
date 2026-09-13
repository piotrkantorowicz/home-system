namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollectionDefinition.Name)]
public sealed class GoalEndpointsTests
{
    private readonly HttpClient _client;

    public GoalEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    [Fact]
    public async Task GET_Goals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/goals");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Goal_WithValidRequest_Returns201()
    {
        var request = new GoalRequest(2000, 150m, 250m, 70m, 30m);

        var response = await _client.PostAsJsonAsync("/api/v1/goals", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
