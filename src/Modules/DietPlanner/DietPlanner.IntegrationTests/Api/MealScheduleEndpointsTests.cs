namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>MealSchedule</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class MealScheduleEndpointsTests
{
    private readonly HttpClient _client;
    private readonly DatabaseFixture _db;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
    public MealScheduleEndpointsTests(DatabaseFixture db)
    {
        _db = db;
        _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();
    }

    /// <summary><c>GET</c> when no schedule exists returns 200 with null body.</summary>
    [Fact]
    public async Task GET_WhenNoScheduleExists_Returns200WithNullBody()
    {
        // Use a unique user ID to guarantee no schedule exists in the shared DB
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var response = await freshClient.GetAsync("/api/v1/meal-schedule");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // TypedResults.Ok(null) serializes as an empty body
        body.ShouldBeOneOf("null", string.Empty);
    }

    /// <summary><c>PUT</c> with valid slots returns 204.</summary>
    [Fact]
    public async Task PUT_WithValidSlots_Returns204()
    {
        var request = new UpdateMealScheduleRequest(
        [
            new MealSlotRequest(null, "Breakfast", "07:00"),
            new MealSlotRequest(null, "Lunch", "12:00"),
            new MealSlotRequest(null, "Dinner", "18:00")
        ]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary><c>GET</c> after put returns stored schedule.</summary>
    [Fact]
    public async Task GET_AfterPut_ReturnsStoredSchedule()
    {
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var request = new UpdateMealScheduleRequest(
        [
            new MealSlotRequest(null, "Morning", "06:30"),
            new MealSlotRequest(null, "Noon", "13:00")
        ]);

        await freshClient.PutAsJsonAsync("/api/v1/meal-schedule", request);

        var getResponse = await freshClient.GetAsync("/api/v1/meal-schedule");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        MealScheduleConfigDto? dto = await getResponse.Content.ReadFromJsonAsync<MealScheduleConfigDto>();
        dto.ShouldNotBeNull();
        dto.Slots.Count.ShouldBe(2);
        dto.Slots.OrderBy(s => s.SortOrder).First().Name.ShouldBe("Morning");
        dto.Slots.OrderBy(s => s.SortOrder).First().DefaultTime.ShouldBe("06:30");
        dto.UpdatedAt.ShouldBeNull();
    }

    /// <summary><c>PUT</c> called twice updates existing schedule.</summary>
    [Fact]
    public async Task PUT_CalledTwice_UpdatesExistingSchedule()
    {
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var first = new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]);
        var second = new UpdateMealScheduleRequest(
        [
            new MealSlotRequest(null, "Brunch", "10:00"),
            new MealSlotRequest(null, "Supper", "20:00")
        ]);

        await freshClient.PutAsJsonAsync("/api/v1/meal-schedule", first);
        var secondResponse = await freshClient.PutAsJsonAsync("/api/v1/meal-schedule", second);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await freshClient.GetAsync("/api/v1/meal-schedule");
        MealScheduleConfigDto? dto = await getResponse.Content.ReadFromJsonAsync<MealScheduleConfigDto>();
        dto.ShouldNotBeNull();
        dto.Slots.Count.ShouldBe(2);
        dto.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary><c>PUT</c> with zero slots returns 400.</summary>
    [Fact]
    public async Task PUT_WithZeroSlots_Returns400()
    {
        var request = new UpdateMealScheduleRequest([]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>PUT</c> with too many slots returns 400.</summary>
    [Theory]
    [InlineData(9)]
    public async Task PUT_WithTooManySlots_Returns400(int slotCount)
    {
        var slots = Enumerable.Range(1, slotCount)
            .Select(i => new MealSlotRequest(null, $"Slot {i}", "08:00"))
            .ToList();

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", new UpdateMealScheduleRequest(slots));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>PUT</c> with empty slot name returns 400.</summary>
    [Fact]
    public async Task PUT_WithEmptySlotName_Returns400()
    {
        var request = new UpdateMealScheduleRequest([new MealSlotRequest(null, "", "07:00")]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>PUT</c> with invalid slot time returns 400.</summary>
    [Fact]
    public async Task PUT_WithInvalidSlotTime_Returns400()
    {
        var request = new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "not-a-time")]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>GET</c>: user id is read from claims principal.</summary>
    [Fact]
    public async Task GET_UserIdIsReadFromClaimsPrincipal()
    {
        var request = new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]);

        await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        var getResponse = await _client.GetAsync("/api/v1/meal-schedule");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        MealScheduleConfigDto? dto = await getResponse.Content.ReadFromJsonAsync<MealScheduleConfigDto>();
        dto.ShouldNotBeNull();
        dto.UserId.ShouldBe(TestAuthHandler.TestUserId);
    }
}
