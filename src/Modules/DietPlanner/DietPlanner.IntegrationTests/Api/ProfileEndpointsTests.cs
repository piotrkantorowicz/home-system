namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetProfile;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class ProfileEndpointsTests
{
    private readonly HttpClient _client;

    public ProfileEndpointsTests(DatabaseFixture db)
        => _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();

    [Fact]
    public async Task GET_Profile_ReturnsOkOrNotFound()
    {
        var response = await _client.GetAsync("/api/v1/profile");

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

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

    [Fact]
    public async Task POST_Profile_WithInvalidGender_Returns400()
    {
        var request = new ProfileRequest(null, "NotAGender", null, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Profile_WithInvalidActivityLevel_Returns400()
    {
        var request = new ProfileRequest(null, null, null, null, null, "RunningLikeCheetah");

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Profile_WithNegativeHeight_Returns400()
    {
        var request = new ProfileRequest(null, null, -1m, null, null, null);

        var response = await _client.PostAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_Profile_WithInvalidGender_Returns400()
    {
        var request = new ProfileRequest(null, "NotAGender", null, null, null, null);

        var response = await _client.PutAsJsonAsync("/api/v1/profile", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

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
