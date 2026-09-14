namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>Profile</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class ProfileEndpointsTests
{
    private readonly HttpClient _client;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public ProfileEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    /// <summary><c>GET</c> profile returns ok or not found.</summary>
    [Fact]
    public async Task GET_Profile_ReturnsOkOrNotFound()
    {
        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    /// <summary>With valid request: <c>POST</c> profile returns 201.</summary>
    [Fact]
    public async Task POST_Profile_WithValidRequest_Returns201()
    {
        // Skip if profile already exists for test user (shared DB across tests)
        var existing = await _client.GetAsync("/api/v1/profile");
        if (existing.StatusCode == HttpStatusCode.OK)
            return;

        var request = new ProfileRequest(
            new DateOnly(1990, 5, 15), "Male", 180m, 80m, 75m, "ModeratelyActive");

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }

    /// <summary>With invalid gender: <c>POST</c> profile returns 400.</summary>
    [Fact]
    public async Task POST_Profile_WithInvalidGender_Returns400()
    {
        var request = new ProfileRequest(null, "NotAGender", null, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>With invalid activity level: <c>POST</c> profile returns 400.</summary>
    [Fact]
    public async Task POST_Profile_WithInvalidActivityLevel_Returns400()
    {
        var request = new ProfileRequest(null, null, null, null, null, "RunningLikeCheetah");

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>With negative height: <c>POST</c> profile returns 400.</summary>
    [Fact]
    public async Task POST_Profile_WithNegativeHeight_Returns400()
    {
        var request = new ProfileRequest(null, null, -1m, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>With invalid gender: <c>PUT</c> profile returns 400.</summary>
    [Fact]
    public async Task PUT_Profile_WithInvalidGender_Returns400()
    {
        var request = new ProfileRequest(null, "NotAGender", null, null, null, null);

        var response = await _client.PutAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>POST</c> then GET then PUT and profile round trip.</summary>
    [Fact]
    public async Task POST_ThenGET_ThenPUT_ProfileRoundTrip()
    {
        // Skip if profile already exists for test user (shared DB across tests)
        var existingResponse = await _client.GetAsync("/api/v1/profile");
        if (existingResponse.StatusCode == HttpStatusCode.NotFound)
        {
            var createRequest = new ProfileRequest(
                new DateOnly(1985, 3, 20), "Female", 165m, 60m, 55m, "LightlyActive");

            var createResponse = await _client.PostAsJsonAsync("/api/v1/profile", createRequest);
            createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var getResponse = await _client.GetAsync("/api/v1/profile");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        UserProfileDto? dto = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        dto.ShouldNotBeNull();
        dto.UserId.ShouldNotBeNullOrEmpty();

        var updateRequest = new ProfileRequest(null, null, 170m, 65m, 60m, "VeryActive");
        var updateResponse = await _client.PutAsJsonAsync("/api/v1/profile", updateRequest);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
