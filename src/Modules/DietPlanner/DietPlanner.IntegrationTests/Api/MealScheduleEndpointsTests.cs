namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class MealScheduleEndpointsTests
{
    private readonly HttpClient _client;
    private readonly DatabaseFixture _db;

    public MealScheduleEndpointsTests(DatabaseFixture db)
    {
        _db = db;
        _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();
    }

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

    [Fact]
    public async Task PUT_WithZeroSlots_Returns400()
    {
        var request = new UpdateMealScheduleRequest([]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

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

    [Fact]
    public async Task PUT_WithEmptySlotName_Returns400()
    {
        var request = new UpdateMealScheduleRequest([new MealSlotRequest(null, "", "07:00")]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_WithInvalidSlotTime_Returns400()
    {
        var request = new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "not-a-time")]);

        var response = await _client.PutAsJsonAsync("/api/v1/meal-schedule", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

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
