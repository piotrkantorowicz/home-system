namespace DietPlanner.UnitTests.Application.ImportPlanning;

using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Application.ImportPlanning;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>ImportPlanResolver</c>: catalog, schedule and diary repositories are substituted with NSubstitute.</summary>
public sealed class ImportPlanResolverTests
{
    private const string UserId = "user-1";
    private static readonly Guid PersonId = Guid.Parse("0199a1b2-0000-7000-8000-000000000001");
    private static readonly string[] SixSlots = ["Breakfast", "Second breakfast", "Lunch", "Snack", "Dinner", "Supper"];

    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly IRecipeRepository _recipes = Substitute.For<IRecipeRepository>();
    private readonly IMealEntryRepository _meals = Substitute.For<IMealEntryRepository>();
    private readonly IMealScheduleConfigRepository _schedules = Substitute.For<IMealScheduleConfigRepository>();
    private readonly ImportPlanResolver _sut;

    /// <summary>Builds the system under test over an empty catalog and diary.</summary>
    public ImportPlanResolverTests()
    {
        _products.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([]);
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([]);
        _meals.GetByPersonAndDateRangeAsync(PersonId, Arg.Any<DateOnly?>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>()).Returns([]);
        _sut = new ImportPlanResolver(_products, _recipes, _meals, _schedules);
    }

    /// <summary>A whole month of six meals a day with new catalog definitions resolves in strict mode.</summary>
    [Fact]
    public async Task ResolveAsync_StrictMonthOf186MealsWithNewCatalog_ResolvesEverything()
    {
        GivenSchedule(SixSlots);
        var recipes = SixSlots.Select(s => RecipeDef($"{s} bowl", ("Oats", 50m, "g"), ("Milk", 200m, "ml"))).ToList();
        var schedule = Enumerable.Range(1, 31)
            .Select(d => new ImportScheduleDto($"2026-10-{d:00}", [.. SixSlots.Select(s => new ImportMealDto(s, $"{s} bowl", 1m, null))]))
            .ToList();
        var import = new ImportDto([ProductDef("Oats"), ProductDef("Milk", unit: "ml")], recipes, schedule);

        var result = await ResolveAsync(import, strict: true);

        result.Issues.ShouldBeEmpty();
        result.Meals.Count.ShouldBe(186);
        result.Products.ShouldAllBe(p => p.IsNew);
        result.Recipes.ShouldAllBe(r => r.IsNew && r.Ingredients.Count == 2);
        result.CreatesOtherSlot.ShouldBeFalse();
    }

    /// <summary>Ingredients that name a product of the same file point at that product's pre-assigned id.</summary>
    [Fact]
    public async Task ResolveAsync_IngredientNamesBatchProduct_UsesItsAssignedId()
    {
        GivenSchedule("Lunch");
        var import = new ImportDto([ProductDef("Rice")], [RecipeDef("Rice bowl", ("RICE", 80m, "g"))], []);

        var result = await ResolveAsync(import, strict: true);

        result.Recipes.Single().Ingredients.Single().ProductId.ShouldBe(result.Products.Single().Id);
    }

    /// <summary>An ingredient whose product exists nowhere is an indexed error in both modes, never a skipped line.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveAsync_IngredientProductUnknown_ReportsIndexedError(bool strict)
    {
        GivenSchedule("Lunch");
        var import = new ImportDto([], [RecipeDef("Soup", ("Leek", 100m, "g"), ("Ghost", 10m, "g"))], []);

        var result = await ResolveAsync(import, strict);

        ShouldHaveError(result, "recipes[0].ingredients[1].product");
    }

    /// <summary>A meal whose recipe exists nowhere is an indexed error in both modes.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResolveAsync_MealRecipeUnknown_ReportsIndexedError(bool strict)
    {
        GivenSchedule("Lunch");
        var import = new ImportDto([], [], [Day("2026-10-01", ("Lunch", "Phantom")), Day("2026-10-02", ("Lunch", "Phantom"))]);

        var result = await ResolveAsync(import, strict);

        ShouldHaveError(result, "schedule[1].meals[0].recipe");
        result.Meals.ShouldBeEmpty();
    }

    /// <summary>Existing catalog items resolve by name regardless of case and are reused.</summary>
    [Fact]
    public async Task ResolveAsync_MealNamesCatalogRecipe_ReusesIt()
    {
        GivenSchedule("Lunch");
        var existing = ExistingRecipe("Chicken Curry");
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([existing]);
        var import = new ImportDto([], [], [Day("2026-10-01", ("lunch", "chicken curry"))]);

        var result = await ResolveAsync(import, strict: true);

        result.Issues.ShouldBeEmpty();
        result.Meals.Single().RecipeId.ShouldBe(existing.Id);
        result.Meals.Single().SlotName.ShouldBe("Lunch");
    }

    /// <summary>Two catalog products matching one name are an ambiguity: an error when strict, a warning otherwise.</summary>
    [Theory]
    [InlineData(true, "error")]
    [InlineData(false, "warning")]
    public async Task ResolveAsync_CatalogNameAmbiguous_ReportsBySeverity(bool strict, string severity)
    {
        GivenSchedule("Lunch");
        _products.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>())
            .Returns([ExistingProduct("Tofu"), ExistingProduct("TOFU")]);
        var import = new ImportDto([], [RecipeDef("Stir fry", ("tofu", 150m, "g"))], []);

        var result = await ResolveAsync(import, strict);

        result.Issues.ShouldContain(i => i.Severity == severity && i.Category == "conflict" && i.Path == "recipes[0].ingredients[0].product");
    }

    /// <summary>A same-name product with different nutrition is reused unchanged and the difference is called out.</summary>
    [Fact]
    public async Task ResolveAsync_ExistingProductDiffers_WarnsAndReuses()
    {
        GivenSchedule("Lunch");
        var existing = ExistingProduct("Butter", calories: 717m);
        _products.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([existing]);

        var result = await ResolveAsync(new ImportDto([ProductDef("Butter", calories: 700m)], [], []), strict: true);

        result.Products.Single().ShouldSatisfyAllConditions(p => p.IsNew.ShouldBeFalse(), p => p.Id.ShouldBe(existing.Id));
        result.Issues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            i => i.Severity.ShouldBe("warning"),
            i => i.Message.ShouldContain("caloriesPer100g"));
    }

    /// <summary>Dates must be ISO calendar days.</summary>
    [Theory]
    [InlineData("2026/10/01")]
    [InlineData("01-10-2026")]
    [InlineData("2026-02-30")]
    public async Task ResolveAsync_DateNotIso_ReportsIndexedError(string date)
    {
        GivenSchedule("Lunch");

        var result = await ResolveAsync(new ImportDto([], [], [new ImportScheduleDto(date, [])]), strict: false);

        ShouldHaveError(result, "schedule[0].date");
    }

    /// <summary>An unsupported unit is an error when strict and a warning in legacy mode.</summary>
    [Theory]
    [InlineData(true, "error")]
    [InlineData(false, "warning")]
    public async Task ResolveAsync_UnitUnsupported_ReportsBySeverity(bool strict, string severity)
    {
        GivenSchedule("Lunch");
        var import = new ImportDto([ProductDef("Flour")], [RecipeDef("Bread", ("Flour", 2m, "handful"))], []);

        var result = await ResolveAsync(import, strict);

        result.Issues.ShouldContain(i => i.Severity == severity && i.Path == "recipes[0].ingredients[0].unit");
    }

    /// <summary>Without a schedule, strict mode rejects the plan; legacy mode warns that a default one will be created.</summary>
    [Theory]
    [InlineData(true, "error")]
    [InlineData(false, "warning")]
    public async Task ResolveAsync_ScheduleMissing_ReportsBySeverity(bool strict, string severity)
    {
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([ExistingRecipe("Toast")]);

        var result = await ResolveAsync(new ImportDto([], [], [Day("2026-10-01", ("Breakfast", "Toast"))]), strict);

        result.Issues.ShouldContain(i => i.Severity == severity && i.Path == "schedule");
        result.Schedule.ShouldBeNull();
    }

    /// <summary>An unknown meal type is an error when strict; legacy mode plans it into a new "Other" slot.</summary>
    [Theory]
    [InlineData(true, "error")]
    [InlineData(false, "warning")]
    public async Task ResolveAsync_MealTypeUnknown_ReportsBySeverity(bool strict, string severity)
    {
        GivenSchedule("Lunch");
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([ExistingRecipe("Toast")]);

        var result = await ResolveAsync(new ImportDto([], [], [Day("2026-10-01", ("brunch", "Toast"))]), strict);

        result.Issues.ShouldContain(i => i.Severity == severity && i.Path == "schedule[0].meals[0].type");
        result.CreatesOtherSlot.ShouldBeTrue();
    }

    /// <summary>A slot that already holds a planned meal is a conflict when strict, unless appending is chosen.</summary>
    [Theory]
    [InlineData(true, false, "error")]
    [InlineData(true, true, "warning")]
    [InlineData(false, false, "warning")]
    public async Task ResolveAsync_SlotOccupied_ReportsBySeverity(bool strict, bool append, string severity)
    {
        var schedule = GivenSchedule("Lunch");
        var recipe = ExistingRecipe("Toast");
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([recipe]);
        var occupied = MealEntry.Create(MealEntryId.New(), PersonId, new DateOnly(2026, 10, 2), schedule.Slots.Single().Id,
            recipe.Id, 1m, null, null, null, TestClock.UtcNow);
        _meals.GetByPersonAndDateRangeAsync(PersonId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2), Arg.Any<CancellationToken>())
            .Returns([occupied]);
        var import = new ImportDto([], [], [Day("2026-10-01", ("Lunch", "Toast")), Day("2026-10-02", ("Lunch", "Toast"))]);

        var result = await _sut.ResolveAsync(import, UserId, PersonId, strict, append, TestContext.Current.CancellationToken);

        result.Issues.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            i => i.Severity.ShouldBe(severity),
            i => i.Category.ShouldBe("conflict"),
            i => i.Path.ShouldBe("schedule[1].meals[0].type"));
    }

    /// <summary>Strict mode bounds a plan to 31 consecutive days; legacy mode does not.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ResolveAsync_ScheduleSpans32Days_RejectsOnlyWhenStrict(bool strict, bool rejected)
    {
        GivenSchedule("Lunch");
        _recipes.GetByNamesAsync(Arg.Any<IReadOnlyCollection<string>>(), UserId, Arg.Any<CancellationToken>()).Returns([ExistingRecipe("Toast")]);
        var import = new ImportDto([], [], [Day("2026-10-01", ("Lunch", "Toast")), Day("2026-11-01", ("Lunch", "Toast"))]);

        var result = await ResolveAsync(import, strict);

        result.HasErrors.ShouldBe(rejected);
    }

    /// <summary>An oversized strict payload is rejected before the catalog is queried.</summary>
    [Fact]
    public async Task ResolveAsync_StrictWithTooManyMeals_RejectsWithoutQueryingCatalog()
    {
        var meals = Enumerable.Range(0, ImportPlanResolver.MaxMeals + 1).Select(_ => new ImportMealDto("Lunch", "Toast", 1m, null)).ToList();

        var result = await ResolveAsync(new ImportDto([], [], [new ImportScheduleDto("2026-10-01", meals)]), strict: true);

        ShouldHaveError(result, "schedule");
        await _recipes.DidNotReceiveWithAnyArgs().GetByNamesAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    /// <summary>A repeated product name keeps the last definition; strict mode rejects the repetition.</summary>
    [Theory]
    [InlineData(true, "error")]
    [InlineData(false, "warning")]
    public async Task ResolveAsync_ProductRepeated_KeepsLastDefinition(bool strict, string severity)
    {
        GivenSchedule("Lunch");

        var result = await ResolveAsync(new ImportDto([ProductDef("Egg", calories: 1m), ProductDef("egg", calories: 155m)], [], []), strict);

        result.Products.ShouldHaveSingleItem().Definition.CaloriesPer100g.ShouldBe(155m);
        result.Issues.ShouldContain(i => i.Severity == severity && i.Path == "products[1].name");
    }

    private Task<ResolvedImport> ResolveAsync(ImportDto import, bool strict)
        => _sut.ResolveAsync(import, UserId, PersonId, strict, appendToOccupiedSlots: false, TestContext.Current.CancellationToken);

    private MealScheduleConfig GivenSchedule(params string[] slots)
    {
        var schedule = MealScheduleConfig.Create(
            MealScheduleConfigId.New(), PersonId, [.. slots.Select((s, i) => (s, new TimeOnly(7 + i, 0)))], TestClock.UtcNow);
        _schedules.GetByPersonIdAsync(PersonId, Arg.Any<CancellationToken>()).Returns(schedule);
        return schedule;
    }

    private static void ShouldHaveError(ResolvedImport result, string path)
    {
        result.HasErrors.ShouldBeTrue();
        result.Issues.ShouldContain(i => i.Severity == "error" && i.Path == path);
    }

    private static ImportProductDto ProductDef(string name, decimal? calories = 100m, string? unit = "g")
        => new(name, calories, 1m, 1m, 1m, null, unit, null, null);

    private static ImportRecipeDto RecipeDef(string name, params (string Product, decimal Amount, string Unit)[] ingredients)
        => new(name, null, null, 1, null, [.. ingredients.Select(i => new ImportIngredientDto(i.Product, i.Amount, i.Unit))]);

    private static ImportScheduleDto Day(string date, params (string Type, string Recipe)[] meals)
        => new(date, [.. meals.Select(m => new ImportMealDto(m.Type, m.Recipe, 1m, null))]);

    private static Product ExistingProduct(string name, decimal? calories = 100m)
        => Product.Create(ProductId.New(), name, new NutritionPer100g(calories, 1m, 1m, 1m, null), "g", null, null, UserId, TestClock.UtcNow);

    private static Recipe ExistingRecipe(string name)
        => Recipe.Create(RecipeId.New(), name, null, null, 1, null, UserId, TestClock.UtcNow);
}
