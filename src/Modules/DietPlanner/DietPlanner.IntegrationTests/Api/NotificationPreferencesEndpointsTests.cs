namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetNotificationPreferences;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollection.Name)]
public sealed class NotificationPreferencesEndpointsTests
{
    private readonly HttpClient _client;
    private readonly DatabaseFixture _db;

    public NotificationPreferencesEndpointsTests(DatabaseFixture db)
    {
        _db = db;
        _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();
    }

    [Fact]
    public async Task GET_WhenNoPreferencesExist_Returns404()
    {
        // Use a unique user ID to guarantee no preferences exist in the shared DB
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var response = await freshClient.GetAsync("/api/v1/notification-preferences");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_WithValidRequest_Returns204()
    {
        var request = new NotificationPreferencesRequest(
            MealReminderEnabled: true,
            MealReminderLeadTimeMinutes: 15,
            WaterReminderEnabled: true,
            WaterReminderIntervalMinutes: 60,
            WeeklySummaryEnabled: true,
            GoalMilestoneAlertsEnabled: true);

        var response = await _client.PutAsJsonAsync("/api/v1/notification-preferences", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_AfterPut_ReturnsStoredPreferences()
    {
        var request = new NotificationPreferencesRequest(
            MealReminderEnabled: false,
            MealReminderLeadTimeMinutes: 30,
            WaterReminderEnabled: false,
            WaterReminderIntervalMinutes: 120,
            WeeklySummaryEnabled: false,
            GoalMilestoneAlertsEnabled: false);

        await _client.PutAsJsonAsync("/api/v1/notification-preferences", request);

        var getResponse = await _client.GetAsync("/api/v1/notification-preferences");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        NotificationPreferencesDto? dto = await getResponse.Content.ReadFromJsonAsync<NotificationPreferencesDto>();
        dto.ShouldNotBeNull();
        dto.MealReminderEnabled.ShouldBeFalse();
        dto.MealReminderLeadTimeMinutes.ShouldBe(30);
        dto.WaterReminderEnabled.ShouldBeFalse();
        dto.WaterReminderIntervalMinutes.ShouldBe(120);
        dto.WeeklySummaryEnabled.ShouldBeFalse();
        dto.GoalMilestoneAlertsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task PUT_CalledTwice_UpdatesExistingPreferences()
    {
        var first = new NotificationPreferencesRequest(
            MealReminderEnabled: true,
            MealReminderLeadTimeMinutes: 15,
            WaterReminderEnabled: true,
            WaterReminderIntervalMinutes: 60,
            WeeklySummaryEnabled: true,
            GoalMilestoneAlertsEnabled: true);

        var second = new NotificationPreferencesRequest(
            MealReminderEnabled: false,
            MealReminderLeadTimeMinutes: 60,
            WaterReminderEnabled: false,
            WaterReminderIntervalMinutes: 240,
            WeeklySummaryEnabled: false,
            GoalMilestoneAlertsEnabled: false);

        await _client.PutAsJsonAsync("/api/v1/notification-preferences", first);
        var secondResponse = await _client.PutAsJsonAsync("/api/v1/notification-preferences", second);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/v1/notification-preferences");
        NotificationPreferencesDto? dto = await getResponse.Content.ReadFromJsonAsync<NotificationPreferencesDto>();
        dto.ShouldNotBeNull();
        dto.MealReminderEnabled.ShouldBeFalse();
        dto.MealReminderLeadTimeMinutes.ShouldBe(60);
        dto.WaterReminderIntervalMinutes.ShouldBe(240);
        dto.UpdatedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task PUT_WithInvalidMealLeadTimeMinutes_Returns400(int leadTime)
    {
        var request = new NotificationPreferencesRequest(
            MealReminderEnabled: true,
            MealReminderLeadTimeMinutes: leadTime,
            WaterReminderEnabled: true,
            WaterReminderIntervalMinutes: 60,
            WeeklySummaryEnabled: true,
            GoalMilestoneAlertsEnabled: true);

        var response = await _client.PutAsJsonAsync("/api/v1/notification-preferences", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    public async Task PUT_WithInvalidWaterReminderIntervalMinutes_Returns400(int interval)
    {
        var request = new NotificationPreferencesRequest(
            MealReminderEnabled: true,
            MealReminderLeadTimeMinutes: 15,
            WaterReminderEnabled: true,
            WaterReminderIntervalMinutes: interval,
            WeeklySummaryEnabled: true,
            GoalMilestoneAlertsEnabled: true);

        var response = await _client.PutAsJsonAsync("/api/v1/notification-preferences", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_UserIdIsReadFromClaimsPrincipal()
    {
        // The endpoint reads UserId from ClaimsPrincipal — TestAuthHandler provides "test-user-001".
        // PUT followed by GET must return the same user's data.
        var request = new NotificationPreferencesRequest(
            MealReminderEnabled: true,
            MealReminderLeadTimeMinutes: 20,
            WaterReminderEnabled: true,
            WaterReminderIntervalMinutes: 45,
            WeeklySummaryEnabled: true,
            GoalMilestoneAlertsEnabled: false);

        await _client.PutAsJsonAsync("/api/v1/notification-preferences", request);

        var getResponse = await _client.GetAsync("/api/v1/notification-preferences");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        NotificationPreferencesDto? dto = await getResponse.Content.ReadFromJsonAsync<NotificationPreferencesDto>();
        dto.ShouldNotBeNull();
        dto.UserId.ShouldBe(TestAuthHandler.TestUserId);
    }
}
