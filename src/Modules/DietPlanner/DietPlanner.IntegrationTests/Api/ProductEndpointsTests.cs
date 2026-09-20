namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Product</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class ProductEndpointsTests
{
    private readonly HttpClient _client;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public ProductEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    /// <summary><c>GET</c> products returns ok.</summary>
    [Fact]
    public async Task GET_Products_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/products", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    /// <summary>With valid request: <c>POST</c> product returns 201.</summary>
    [Fact]
    public async Task POST_Product_WithValidRequest_Returns201()
    {
        var request = new CreateProductRequest("Test Chicken", 165m, 31m, 0m, 3.6m, 0m, "g", null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/products", request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    /// <summary>When not found: <c>GET</c> product by id returns 404.</summary>
    [Fact]
    public async Task GET_ProductById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/products/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, body);
    }

    /// <summary>When exists: <c>DELETE</c> product returns 204.</summary>
    [Fact]
    public async Task DELETE_Product_WhenExists_Returns204()
    {
        // Arrange — create first
        var createReq = new CreateProductRequest("DeleteMe", null, null, null, null, null, "g", null, null);
        var createResp = await _client.PostAsJsonAsync("/api/v1/products", createReq, cancellationToken: TestContext.Current.CancellationToken);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = createResp.Headers.Location!.ToString();
        var id = location.Split('/').Last();

        // Act
        var deleteResp = await _client.DeleteAsync($"/api/v1/products/{id}", TestContext.Current.CancellationToken);

        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
