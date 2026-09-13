namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetDietReminderSettings;
using DietPlanner.IntegrationTests.Infrastructure;

[Collection(DatabaseCollectionDefinition.Name)]
public sealed class DietReminderSettingsEndpointsTests
{
    private const string Path = "/api/v1/diet-reminder-settings";

    private readonly HttpClient _client;
    private readonly DatabaseFixture _db;

    public DietReminderSettingsEndpointsTests(DatabaseFixture db)
    {
        _db = db;
        _client = new DietPlannerWebApplicationFactory(db.ConnectionString).CreateClient();
    }

    private static DietReminderSettingsRequest DefaultRequest() => new(
        MealRemindersEnabled: true,
        MealReminderLeadTimeMinutes: 15,
        MealMissedGraceMinutes: 30,
        WaterRemindersEnabled: true,
        WaterReminderIntervalMinutes: 60,
        WaterWindowStartUtc: new TimeOnly(6, 0),
        WaterWindowEndUtc: new TimeOnly(22, 0),
        WeeklySummaryEnabled: true,
        WeeklySummaryDayOfWeekUtc: DayOfWeek.Sunday,
        WeeklySummaryTimeOfDayUtc: new TimeOnly(8, 0),
        GoalAlertsEnabled: true);

    [Fact]
    public async Task GET_WhenNoSettingsExist_Returns404()
    {
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var response = await freshClient.GetAsync(Path);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_WithValidRequest_Returns204()
    {
        var response = await _client.PutAsJsonAsync(Path, DefaultRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_AfterPut_ReturnsStoredSettings()
    {
        var request = DefaultRequest() with
        {
            MealRemindersEnabled = false,
            MealReminderLeadTimeMinutes = 30,
            MealMissedGraceMinutes = 45,
            WaterReminderIntervalMinutes = 120,
            WaterWindowStartUtc = new TimeOnly(7, 0),
            WaterWindowEndUtc = new TimeOnly(21, 0),
            WeeklySummaryDayOfWeekUtc = DayOfWeek.Monday,
            WeeklySummaryTimeOfDayUtc = new TimeOnly(9, 0),
            GoalAlertsEnabled = false,
        };

        await _client.PutAsJsonAsync(Path, request);

        var getResponse = await _client.GetAsync(Path);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>();
        dto.ShouldNotBeNull();
        dto.MealRemindersEnabled.ShouldBeFalse();
        dto.MealReminderLeadTimeMinutes.ShouldBe(30);
        dto.MealMissedGraceMinutes.ShouldBe(45);
        dto.WaterReminderIntervalMinutes.ShouldBe(120);
        dto.WaterWindowStartUtc.ShouldBe(new TimeOnly(7, 0));
        dto.WaterWindowEndUtc.ShouldBe(new TimeOnly(21, 0));
        dto.WeeklySummaryDayOfWeekUtc.ShouldBe(DayOfWeek.Monday);
        dto.WeeklySummaryTimeOfDayUtc.ShouldBe(new TimeOnly(9, 0));
        dto.GoalAlertsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task PUT_CalledTwice_UpdatesExistingSettings()
    {
        await _client.PutAsJsonAsync(Path, DefaultRequest());
        var second = DefaultRequest() with
        {
            MealReminderLeadTimeMinutes = 60,
            WaterReminderIntervalMinutes = 240,
        };
        var secondResponse = await _client.PutAsJsonAsync(Path, second);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(Path);
        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>();
        dto.ShouldNotBeNull();
        dto.MealReminderLeadTimeMinutes.ShouldBe(60);
        dto.WaterReminderIntervalMinutes.ShouldBe(240);
        dto.UpdatedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task PUT_WithInvalidMealLeadTimeMinutes_Returns400(int leadTime)
    {
        var request = DefaultRequest() with { MealReminderLeadTimeMinutes = leadTime };

        var response = await _client.PutAsJsonAsync(Path, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    public async Task PUT_WithInvalidWaterReminderIntervalMinutes_Returns400(int interval)
    {
        var request = DefaultRequest() with { WaterReminderIntervalMinutes = interval };

        var response = await _client.PutAsJsonAsync(Path, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_WithWaterWindowEndBeforeStart_Returns400()
    {
        var request = DefaultRequest() with
        {
            WaterWindowStartUtc = new TimeOnly(12, 0),
            WaterWindowEndUtc = new TimeOnly(11, 0),
        };

        var response = await _client.PutAsJsonAsync(Path, request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_UserIdIsReadFromClaimsPrincipal()
    {
        await _client.PutAsJsonAsync(Path, DefaultRequest());

        var getResponse = await _client.GetAsync(Path);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>();
        dto.ShouldNotBeNull();
        dto.UserId.ShouldBe(TestAuthHandler.TestUserId);
    }
}
