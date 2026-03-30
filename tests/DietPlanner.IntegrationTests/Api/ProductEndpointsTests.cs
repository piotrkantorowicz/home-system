namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class ProductEndpointsTests : IClassFixture<DietPlannerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    [Fact]
    public async Task GET_Products_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Product_WithValidRequest_Returns201()
    {
        var request = new CreateProductRequest("Test Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GET_ProductById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Product_WhenExists_Returns204()
    {
        // Arrange — create first
        var createReq = new CreateProductRequest("DeleteMe", null, null, null, null, null, "g", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/v1/products", createReq);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = createResp.Headers.Location!.ToString();
        var id = location.Split('/').Last();

        // Act
        var deleteResp = await _client.DeleteAsync($"/api/v1/products/{id}");

        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
