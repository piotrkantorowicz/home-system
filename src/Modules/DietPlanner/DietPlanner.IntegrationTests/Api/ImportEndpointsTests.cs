namespace DietPlanner.IntegrationTests.Api;

using System.Net;
using System.Net.Http.Json;
using DietPlanner.Api;
using DietPlanner.Application.Commands.ExecuteImport;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.Queries.GetMealEntries;
using DietPlanner.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Cqrs;

/// <summary>Integration tests for bulk import: HTTP (legacy SPA path) and dispatcher (strict path) → resolver → PostgreSQL (Testcontainers).</summary>
[Collection(DatabaseCollectionDefinition.Name)]
public sealed class ImportEndpointsTests(DatabaseFixture db)
{
    private static readonly string[] SixSlots = ["Breakfast", "Second breakfast", "Lunch", "Snack", "Dinner", "Supper"];

    /// <summary>The SPA flow still validates and imports a small plan, creating the default schedule for a new user.</summary>
    [Fact]
    public async Task POST_ValidateThenImport_LegacyPlan_CreatesCatalogAndMeals()
    {
        using var factory = Factory(out var client);
        var import = SmallPlan("Lunch", "Legacy bowl");

        var validation = await client.PostAsJsonAsync("/api/v1/meals/validate", import, TestContext.Current.CancellationToken);
        var preview = await validation.Content.ReadFromJsonAsync<ValidationResultDto>(TestContext.Current.CancellationToken);
        var response = await client.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);

        preview!.CanProceed.ShouldBeTrue();
        preview.Issues.ShouldContain(i => i.Severity == "warning" && i.Path == "schedule");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result!.Stats.ShouldBe(new ImportStatsDto(1, 0, 1, 0, 1));
        (await MealsAsync(client, "2026-10-01", "2026-10-01")).ShouldHaveSingleItem();
    }

    /// <summary>An unresolved recipe now rejects the import with 400 and writes nothing; it used to be skipped with 201.</summary>
    [Fact]
    public async Task POST_Import_WithUnresolvedRecipe_Returns400AndWritesNothing()
    {
        using var factory = Factory(out var client);
        var import = new ImportDto(
            [new ImportProductDto("Orphan product", 100m, null, null, null, null, "g", null, null)],
            [],
            [new ImportScheduleDto("2026-10-01", [new ImportMealDto("Lunch", "Nonexistent recipe", 1m, null)])]);

        var response = await client.PostAsJsonAsync("/api/v1/meals/import", import, TestContext.Current.CancellationToken);
        var products = await client.GetStringAsync("/api/v1/products?Search=Orphan", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("schedule[0].meals[0].recipe");
        products.ShouldNotContain("Orphan product");
    }

    /// <summary>Names resolve against the database ignoring case, so a re-import reuses instead of duplicating.</summary>
    [Fact]
    public async Task POST_Import_WithDifferentlyCasedNames_ReusesCatalog()
    {
        using var factory = Factory(out var client);
        await EnsureScheduleAsync(client, "Lunch");
        (await client.PostAsJsonAsync("/api/v1/meals/import", SmallPlan("Lunch", "Case bowl"), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        var again = new ImportDto(
            [new ImportProductDto("CASE BOWL RICE", 130m, 2.7m, 28m, 0.3m, null, "g", null, null)],
            [new ImportRecipeDto("case BOWL", null, null, 1, null, [new ImportIngredientDto("case bowl rice", 80m, "g")])],
            [new ImportScheduleDto("2026-10-02", [new ImportMealDto("lunch", "Case Bowl", 1m, null)])]);
        var response = await client.PostAsJsonAsync("/api/v1/meals/import", again, TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result!.Stats.ShouldBe(new ImportStatsDto(0, 1, 0, 1, 1));
    }

    /// <summary>Catalog names are unique per owner, so another user's same-name product and recipe do not block an import.</summary>
    [Fact]
    public async Task POST_Import_WithNamesOwnedByAnotherUser_CreatesOwnCopies()
    {
        var recipe = $"Shared name bowl {Guid.CreateVersion7().ToString("N")[^8..]}";
        using var partner = Factory(out var partnerClient);
        (await partnerClient.PostAsJsonAsync("/api/v1/meals/import", SmallPlan("Lunch", recipe), TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();
        using var factory = Factory(out var client);

        var response = await client.PostAsJsonAsync("/api/v1/meals/import", SmallPlan("Lunch", recipe), TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ImportResultDto>(TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        result!.Stats.ShouldBe(new ImportStatsDto(1, 0, 1, 0, 1));
    }

    /// <summary>A strict month of 186 meals with new catalog definitions commits in full; importing it again conflicts on every occupied slot.</summary>
    [Fact]
    public async Task StrictImport_FullMonth_CommitsAllMealsThenRejectsOccupiedSlots()
    {
        var userId = $"import-month-{Guid.CreateVersion7()}";
        using var factory = Factory(out var client, userId);
        await EnsureScheduleAsync(client, SixSlots);
        var import = new ImportDto(
            [new ImportProductDto("Month oats", 380m, 13m, 60m, 7m, 10m, "g", null, null),
             new ImportProductDto("Month milk", 64m, 3.3m, 4.8m, 3.6m, null, "ml", 1.03m, null)],
            [.. SixSlots.Select(s => new ImportRecipeDto($"{s} month bowl", null, null, 1, null,
                [new ImportIngredientDto("Month oats", 50m, "g"), new ImportIngredientDto("Month milk", 200m, "ml")]))],
            [.. Enumerable.Range(1, 31).Select(d => new ImportScheduleDto(
                $"2026-10-{d:00}", [.. SixSlots.Select(s => new ImportMealDto(s, $"{s} month bowl", 1m, null))]))]);
        var command = new ExecuteImportCommand(import, userId, TestAuthHandler.PersonIdFor(userId), Strict: true);

        var result = await SendAsync(factory, command);
        var meals = await MealsAsync(client, "2026-10-01", "2026-10-31");
        var retry = await Should.ThrowAsync<CommandValidationException>(() => SendAsync(factory, command));

        result.Stats.ShouldBe(new ImportStatsDto(2, 0, 6, 0, 186));
        meals.Count.ShouldBe(186);
        meals.ShouldAllBe(m => m.Status == "Planned");
        retry.Errors.Count.ShouldBe(186);
        (await MealsAsync(client, "2026-10-01", "2026-10-31")).Count.ShouldBe(186);
    }

    /// <summary>Strict validation reports a missing schedule and an unknown meal type as errors instead of planning side effects.</summary>
    [Fact]
    public async Task StrictValidate_WithoutSchedule_ReportsErrorsAndNoPlan()
    {
        var userId = $"import-strict-{Guid.CreateVersion7()}";
        using var factory = Factory(out _, userId);
        var command = new ValidateImportCommand(SmallPlan("Brunch", "Strict bowl"), userId, TestAuthHandler.PersonIdFor(userId), Strict: true);

        using var scope = factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .SendAsync<ValidateImportCommand, ValidationResultDto>(command, TestContext.Current.CancellationToken);

        result.CanProceed.ShouldBeFalse();
        result.Plan.ShouldBeNull();
        result.Issues.ShouldContain(i => i.Severity == "error" && i.Path == "schedule");
        result.Issues.ShouldContain(i => i.Severity == "error" && i.Path == "schedule[0].meals[0].type");
    }

    private DietPlannerWebApplicationFactory Factory(out HttpClient client, string? userId = null)
    {
        var factory = new DietPlannerWebApplicationFactory(db.ConnectionString, userId ?? $"import-{Guid.CreateVersion7()}");
        client = factory.CreateClient();
        return factory;
    }

    private static ImportDto SmallPlan(string mealType, string recipe)
        => new(
            [new ImportProductDto($"{recipe} rice", 130m, 2.7m, 28m, 0.3m, null, "g", null, null)],
            [new ImportRecipeDto(recipe, null, null, 1, null, [new ImportIngredientDto($"{recipe} rice", 80m, "g")])],
            [new ImportScheduleDto("2026-10-01", [new ImportMealDto(mealType, recipe, 1m, null)])]);

    private static async Task EnsureScheduleAsync(HttpClient client, params string[] slots)
    {
        var request = new UpdateMealScheduleRequest([.. slots.Select((s, i) => new MealSlotRequest(null, s, $"{7 + i:00}:00"))]);
        (await client.PutAsJsonAsync("/api/v1/meal-schedule", request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
    }

    private static async Task<IReadOnlyList<MealEntryDto>> MealsAsync(HttpClient client, string from, string to)
        => (await client.GetFromJsonAsync<IReadOnlyList<MealEntryDto>>(
            $"/api/v1/meals?from={from}&to={to}", TestContext.Current.CancellationToken))!;

    private static async Task<ImportResultDto> SendAsync(DietPlannerWebApplicationFactory factory, ExecuteImportCommand command)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandDispatcher>()
            .SendAsync<ExecuteImportCommand, ImportResultDto>(command, TestContext.Current.CancellationToken);
    }
}
