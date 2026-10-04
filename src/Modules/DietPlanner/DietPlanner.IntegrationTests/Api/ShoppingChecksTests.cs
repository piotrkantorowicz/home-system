namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Queries.GetMealSchedule;
using DietPlanner.Application.Queries.GetShoppingList;
using DietPlanner.IntegrationTests.Infrastructure;
using global::Household.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>#488 — shopping-list check-offs are shared by the household, keyed by range + product + unit, and idempotent.</summary>
public sealed class ShoppingChecksTests : IClassFixture<HouseholdShoppingListFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly HouseholdShoppingListFixture _fx;

    /// <summary>Creates the test class instance for one test, wired to the shared fixture.</summary>
    /// <param name="fx">The shared fixture.</param>
    public ShoppingChecksTests(HouseholdShoppingListFixture fx) => _fx = fx;

    private DietPlannerWebApplicationFactory FactoryFor(string subject)
        => new(
            _fx.DietPlannerConnectionString,
            subject,
            new Dictionary<string, string?> { ["ConnectionStrings:Household"] = _fx.HouseholdConnectionString });

    private async Task<(HttpClient Owner, HttpClient Member, HttpClient Guest)> HouseholdAsync()
    {
        var ownerFactory = FactoryFor($"chk-owner-{Guid.NewGuid():N}");
        await ownerFactory.Services.MigrateHouseholdDatabaseAsync(NullLogger.Instance);
        var owner = ownerFactory.CreateClient();
        (await owner.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        (await owner.PostAsJsonAsync("/api/households", new { name = "Checks" }, Ct)).EnsureSuccessStatusCode();
        var householdId = (await owner.GetFromJsonAsync<IdBody>("/api/households/me", Ct))!.Id;

        var member = await JoinAsync(owner, householdId, "Adult");
        var guest = await JoinAsync(owner, householdId, "Guest");
        return (owner, member, guest);
    }

    private async Task<HttpClient> JoinAsync(HttpClient owner, Guid householdId, string role)
    {
        var client = FactoryFor($"chk-{role}-{Guid.NewGuid():N}").CreateClient();
        (await client.PostAsync("/api/persons/me/sync", null, Ct)).EnsureSuccessStatusCode();
        var personId = (await client.GetFromJsonAsync<IdBody>("/api/persons/me", Ct))!.Id;
        var add = await owner.PostAsJsonAsync($"/api/households/{householdId}/members", new { personId, role, nickname = (string?)null }, Ct);
        add.EnsureSuccessStatusCode();
        var invitation = (await add.Content.ReadFromJsonAsync<InvitationBody>(Ct))!.InvitationId;
        (await client.PostAsync($"/api/households/invitations/{invitation}/accept", null, Ct)).EnsureSuccessStatusCode();
        return client;
    }

    /// <summary>Plans one meal per (unit, amount) line, all of one new product, and returns the product id.</summary>
    private static async Task<Guid> PlanAsync(HttpClient client, DateOnly date, params (string Unit, decimal Amount)[] lines)
    {
        (await client.PutAsJsonAsync("/api/v1/meal-schedule", new UpdateMealScheduleRequest([new MealSlotRequest(null, "Breakfast", "07:00")]), Ct))
            .EnsureSuccessStatusCode();
        var slotId = (await client.GetFromJsonAsync<MealScheduleConfigDto>("/api/v1/meal-schedule", Ct))!.Slots[0].Id;
        var product = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest($"chk-{Guid.NewGuid():N}", 100m, 5m, 10m, 2m, 1m, "g", null, null), Ct);
        product.EnsureSuccessStatusCode();
        var productId = Guid.Parse((await product.Content.ReadAsStringAsync(Ct)).Trim('"'));

        foreach (var (unit, amount) in lines)
        {
            var recipe = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeRequest(
                $"chk-r-{Guid.NewGuid():N}", null, null, 1, 5, [new RecipeIngredientRequest(productId, amount, unit)]), Ct);
            recipe.EnsureSuccessStatusCode();
            var recipeId = Guid.Parse((await recipe.Content.ReadAsStringAsync(Ct)).Trim('"'));
            (await client.PostAsJsonAsync("/api/v1/meals", new CreateMealEntryRequest(date, slotId, recipeId, 1m, null, null, 0), Ct)).EnsureSuccessStatusCode();
        }

        return productId;
    }

    private static string Range(DateOnly from, DateOnly to) => $"From={from:yyyy-MM-dd}&To={to:yyyy-MM-dd}";

    private static async Task<List<ShoppingListItemDto>> ListAsync(HttpClient client, DateOnly from, DateOnly to)
        => (await client.GetFromJsonAsync<List<ShoppingListItemDto>>($"/api/v1/meals/shopping-list?{Range(from, to)}", Ct))!;

    private static Task<HttpResponseMessage> TickAsync(HttpClient client, DateOnly from, DateOnly to, Guid product, string unit, bool isChecked)
        => client.PutAsJsonAsync("/api/v1/meals/shopping-list/checks", new SetShoppingCheckRequest(from, to, product, unit, isChecked), Ct);

    /// <summary>A member's tick is visible to the owner; repeating is a no-op; unticking clears it for both.</summary>
    [Fact]
    public async Task Check_IsSharedAndIdempotent()
    {
        var (owner, member, _) = await HouseholdAsync();
        var day = TestClock.Today;
        var product = await PlanAsync(owner, day, ("g", 100m));

        (await TickAsync(member, day, day, product, "g", true)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TickAsync(member, day, day, product, "g", true)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(owner, day, day)).Single().IsChecked.ShouldBeTrue();
        (await ListAsync(member, day, day)).Single().IsChecked.ShouldBeTrue();

        (await TickAsync(owner, day, day, product, "g", false)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TickAsync(owner, day, day, product, "g", false)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(member, day, day)).Single().IsChecked.ShouldBeFalse();
    }

    /// <summary>The same product in two units checks independently, and a tick belongs to its exact range.</summary>
    [Fact]
    public async Task Check_IsPerUnitAndPerRange()
    {
        var (owner, _, _) = await HouseholdAsync();
        var day = TestClock.Today;
        var product = await PlanAsync(owner, day, ("g", 100m), ("ml", 50m));

        await TickAsync(owner, day, day, product, "g", true);

        var items = await ListAsync(owner, day, day);
        items.Single(i => i.Unit == "g").IsChecked.ShouldBeTrue();
        items.Single(i => i.Unit == "ml").IsChecked.ShouldBeFalse();
        (await ListAsync(owner, day, day.AddDays(1))).ShouldAllBe(i => !i.IsChecked);
    }

    /// <summary>Clearing a range unticks every row of that range only.</summary>
    [Fact]
    public async Task Clear_UntickEveryRowOfOneRange()
    {
        var (owner, _, _) = await HouseholdAsync();
        var day = TestClock.Today;
        var product = await PlanAsync(owner, day, ("g", 100m), ("ml", 50m));
        await TickAsync(owner, day, day, product, "g", true);
        await TickAsync(owner, day, day, product, "ml", true);
        await TickAsync(owner, day, day.AddDays(1), product, "g", true);

        var clear = await owner.DeleteAsync($"/api/v1/meals/shopping-list/checks?{Range(day, day)}", Ct);

        clear.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(owner, day, day)).ShouldAllBe(i => !i.IsChecked);
        (await ListAsync(owner, day, day.AddDays(1))).Single(i => i.Unit == "g").IsChecked.ShouldBeTrue();
    }

    /// <summary>Another household never sees or changes the household's ticks; a Guest cannot tick.</summary>
    [Fact]
    public async Task Check_IsIsolatedFromOutsiders_AndGuestsCannotWrite()
    {
        var (owner, _, guest) = await HouseholdAsync();
        var (outsider, _, _) = await HouseholdAsync();
        var day = TestClock.Today;
        var product = await PlanAsync(owner, day, ("g", 100m));
        await TickAsync(owner, day, day, product, "g", true);

        (await TickAsync(guest, day, day, product, "g", false)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await owner.DeleteAsync($"/api/v1/meals/shopping-list/checks?{Range(day, day)}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await TickAsync(owner, day, day, product, "g", true);

        await TickAsync(outsider, day, day, product, "g", false);
        var cleared = await outsider.DeleteAsync($"/api/v1/meals/shopping-list/checks?{Range(day, day)}", Ct);
        cleared.StatusCode.ShouldBe(HttpStatusCode.NoContent, await cleared.Content.ReadAsStringAsync(Ct));
        (await ListAsync(owner, day, day)).Single().IsChecked.ShouldBeTrue();
    }

    /// <summary>Many members ticking the same row at once all succeed and leave one check.</summary>
    [Fact]
    public async Task Check_ConcurrentTicksAllSucceed()
    {
        var (owner, member, _) = await HouseholdAsync();
        var day = TestClock.Today;
        var product = await PlanAsync(owner, day, ("g", 100m));

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => TickAsync(i % 2 == 0 ? owner : member, day, day, product, "g", true)));

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.NoContent);
        (await ListAsync(owner, day, day)).Single().IsChecked.ShouldBeTrue();
    }

    /// <summary>A blank unit or an inverted range is a validation error.</summary>
    [Fact]
    public async Task Check_RejectsInvalidInput()
    {
        var (owner, _, _) = await HouseholdAsync();
        var day = TestClock.Today;

        (await TickAsync(owner, day, day, Guid.NewGuid(), " ", true)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await TickAsync(owner, day.AddDays(1), day, Guid.NewGuid(), "g", true)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record IdBody(Guid Id);
    private sealed record InvitationBody(Guid InvitationId);
}
