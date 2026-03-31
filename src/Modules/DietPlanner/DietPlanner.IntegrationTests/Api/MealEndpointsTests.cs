namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class MealEndpointsTests
{
    private readonly HttpClient _client;

    public MealEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    [Fact]
    public async Task GET_Meals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }

    [Fact]
    public async Task GET_NutritionSummary_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/meals/nutrition-summary");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
    }
}
