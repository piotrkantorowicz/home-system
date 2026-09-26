namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetDietReminderSettings;
using DietPlanner.IntegrationTests.Infrastructure;

/// <summary>HTTP integration tests for the <c>DietReminderSettings</c> endpoints: request → dispatcher → handler → PostgreSQL (Testcontainers) → response.</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class DietReminderSettingsEndpointsTests
{
    private const string Path = "/api/v1/diet-reminder-settings";

    private readonly HttpClient _client;
    private readonly DatabaseFixture _db;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="db">The shared database container fixture.</param>
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
        WaterWindowStart: new TimeOnly(6, 0),
        WaterWindowEnd: new TimeOnly(22, 0),
        WeeklySummaryEnabled: true,
        WeeklySummaryDayOfWeek: DayOfWeek.Sunday,
        WeeklySummaryTimeOfDay: new TimeOnly(8, 0),
        GoalAlertsEnabled: true);

    /// <summary><c>GET</c> when no settings exist returns 404.</summary>
    [Fact]
    public async Task GET_WhenNoSettingsExist_Returns404()
    {
        var freshUserId = Guid.NewGuid().ToString();
        HttpClient freshClient = new DietPlannerWebApplicationFactory(_db.ConnectionString, freshUserId).CreateClient();

        var response = await freshClient.GetAsync(Path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary><c>PUT</c> with valid request returns 204.</summary>
    [Fact]
    public async Task PUT_WithValidRequest_Returns204()
    {
        var response = await _client.PutAsJsonAsync(Path, DefaultRequest(), cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    /// <summary><c>GET</c> after put returns stored settings.</summary>
    [Fact]
    public async Task GET_AfterPut_ReturnsStoredSettings()
    {
        var request = DefaultRequest() with
        {
            MealRemindersEnabled = false,
            MealReminderLeadTimeMinutes = 30,
            MealMissedGraceMinutes = 45,
            WaterReminderIntervalMinutes = 120,
            WaterWindowStart = new TimeOnly(7, 0),
            WaterWindowEnd = new TimeOnly(21, 0),
            WeeklySummaryDayOfWeek = DayOfWeek.Monday,
            WeeklySummaryTimeOfDay = new TimeOnly(9, 0),
            GoalAlertsEnabled = false,
        };

        await _client.PutAsJsonAsync(Path, request, cancellationToken: TestContext.Current.CancellationToken);

        var getResponse = await _client.GetAsync(Path, TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>(cancellationToken: TestContext.Current.CancellationToken);
        dto.ShouldNotBeNull();
        dto.MealRemindersEnabled.ShouldBeFalse();
        dto.MealReminderLeadTimeMinutes.ShouldBe(30);
        dto.MealMissedGraceMinutes.ShouldBe(45);
        dto.WaterReminderIntervalMinutes.ShouldBe(120);
        dto.WaterWindowStart.ShouldBe(new TimeOnly(7, 0));
        dto.WaterWindowEnd.ShouldBe(new TimeOnly(21, 0));
        dto.WeeklySummaryDayOfWeek.ShouldBe(DayOfWeek.Monday);
        dto.WeeklySummaryTimeOfDay.ShouldBe(new TimeOnly(9, 0));
        dto.GoalAlertsEnabled.ShouldBeFalse();
    }

    /// <summary><c>PUT</c> called twice updates existing settings.</summary>
    [Fact]
    public async Task PUT_CalledTwice_UpdatesExistingSettings()
    {
        await _client.PutAsJsonAsync(Path, DefaultRequest(), cancellationToken: TestContext.Current.CancellationToken);
        var second = DefaultRequest() with
        {
            MealReminderLeadTimeMinutes = 60,
            WaterReminderIntervalMinutes = 240,
        };
        var secondResponse = await _client.PutAsJsonAsync(Path, second, cancellationToken: TestContext.Current.CancellationToken);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(Path, TestContext.Current.CancellationToken);
        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>(cancellationToken: TestContext.Current.CancellationToken);
        dto.ShouldNotBeNull();
        dto.MealReminderLeadTimeMinutes.ShouldBe(60);
        dto.WaterReminderIntervalMinutes.ShouldBe(240);
        dto.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary><c>PUT</c> with invalid meal lead time minutes returns 400.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task PUT_WithInvalidMealLeadTimeMinutes_Returns400(int leadTime)
    {
        var request = DefaultRequest() with { MealReminderLeadTimeMinutes = leadTime };

        var response = await _client.PutAsJsonAsync(Path, request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>PUT</c> with invalid water reminder interval minutes returns 400.</summary>
    [Theory]
    [InlineData(14)]
    [InlineData(481)]
    public async Task PUT_WithInvalidWaterReminderIntervalMinutes_Returns400(int interval)
    {
        var request = DefaultRequest() with { WaterReminderIntervalMinutes = interval };

        var response = await _client.PutAsJsonAsync(Path, request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>PUT</c> with water window end before start returns 400.</summary>
    [Fact]
    public async Task PUT_WithWaterWindowEndBeforeStart_Returns400()
    {
        var request = DefaultRequest() with
        {
            WaterWindowStart = new TimeOnly(12, 0),
            WaterWindowEnd = new TimeOnly(11, 0),
        };

        var response = await _client.PutAsJsonAsync(Path, request, cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary><c>GET</c>: user id is read from claims principal.</summary>
    [Fact]
    public async Task GET_UserIdIsReadFromClaimsPrincipal()
    {
        await _client.PutAsJsonAsync(Path, DefaultRequest(), cancellationToken: TestContext.Current.CancellationToken);

        var getResponse = await _client.GetAsync(Path, TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        DietReminderSettingsDto? dto = await getResponse.Content.ReadFromJsonAsync<DietReminderSettingsDto>(cancellationToken: TestContext.Current.CancellationToken);
        dto.ShouldNotBeNull();
        dto.PersonId.ShouldBe(TestAuthHandler.PersonIdFor(TestAuthHandler.TestUserId));
    }
}
