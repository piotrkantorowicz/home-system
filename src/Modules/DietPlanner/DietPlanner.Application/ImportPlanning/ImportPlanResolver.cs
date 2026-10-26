namespace DietPlanner.Application.ImportPlanning;

using System.Globalization;
using DietPlanner.Application.Commands.ValidateImport;
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Validates an import file and resolves every name against the caller's own catalog, meal schedule and
/// diary, so validation and execution see the same plan. Legacy mode keeps the SPA import forgiving
/// (default schedule, "Other" slot, appending to occupied slots) but reports each side effect as a
/// warning; strict mode turns every fallback into an error and bounds the payload to one month.
/// </summary>
internal sealed class ImportPlanResolver(
    IProductRepository productRepository,
    IRecipeRepository recipeRepository,
    IMealEntryRepository mealEntryRepository,
    IMealScheduleConfigRepository scheduleRepository)
{
    public const string Error = "error";
    public const string Warning = "warning";
    public const string Info = "info";

    public const int MaxDays = 31;
    public const int MaxMeals = 250;
    public const int MaxProducts = 250;
    public const int MaxRecipes = 250;
    public const int MaxIngredientsPerRecipe = 100;

    public const string OtherSlot = "Other";
    private const int MaxNameLength = 200;
    private const int MaxSlots = 8;
    private const decimal LegacyDefaultAmount = 100m;

    public static readonly IReadOnlyList<(string Name, TimeOnly DefaultTime)> DefaultMealSlots =
    [
        ("Breakfast", new TimeOnly(7, 0)),
        ("Lunch", new TimeOnly(12, 0)),
        ("Dinner", new TimeOnly(18, 0)),
        ("Snack", new TimeOnly(15, 0)),
    ];

    /// <summary>The validation failure for a resolution with errors: one entry per error, keyed by its JSON path.</summary>
    public static CommandValidationException Rejection(string commandName, ResolvedImport resolved)
        => new(commandName, [.. resolved.Issues
            .Where(i => i.Severity == Error)
            .Select(i => new ValidationError(i.Path ?? "import", i.Message))]);

    private delegate void AddIssue(string severity, string category, string? path, string? item, string message, string? resolution = null);

    public async Task<ResolvedImport> ResolveAsync(
        ImportDto import,
        string userId,
        Guid personId,
        bool strict,
        bool appendToOccupiedSlots,
        CancellationToken ct)
    {
        var issues = new List<ValidationIssueDto>();
        // Severity of anything legacy mode tolerates with a side effect or a guess.
        string fallback = strict ? Error : Warning;

        void Add(string severity, string category, string? path, string? item, string message, string? resolution = null)
            => issues.Add(new ValidationIssueDto(severity, category, path, item, message, resolution));

        var products = import.Products ?? [];
        var recipes = import.Recipes ?? [];
        var days = import.Schedule ?? [];

        if (strict && ExceedsBounds(products.Count, recipes.Count, days.Sum(d => (d.Meals ?? []).Count), Add))
            return new ResolvedImport(issues, [], [], [], null, false, []);

        var productDefs = CollectProducts(products, fallback, Add);
        var recipeDefs = CollectRecipes(recipes, strict, fallback, Add);

        // ── Products: the file's definitions first, then the caller's catalog ──
        var referencedProducts = recipeDefs.Values
            .SelectMany(r => r.Definition.Ingredients ?? [])
            .Where(ing => !string.IsNullOrWhiteSpace(ing.Product))
            .Select(ing => ing.Product);
        var existingProducts = Group(
            await productRepository.GetByNamesAsync(
                [.. productDefs.Keys.Concat(referencedProducts).Distinct(StringComparer.OrdinalIgnoreCase)], userId, ct),
            p => p.Name);

        var productIds = new Dictionary<string, ProductId>(StringComparer.OrdinalIgnoreCase);
        var resolvedProducts = new List<ResolvedProduct>();

        foreach (var (name, (definition, index)) in productDefs)
        {
            var path = $"products[{index}].name";
            var existing = PickExisting(existingProducts, name, path, "Product", fallback, Add);

            if (existing is null)
            {
                var id = ProductId.New();
                resolvedProducts.Add(new ResolvedProduct(id, definition, IsNew: true));
                productIds[name] = id;
                continue;
            }

            resolvedProducts.Add(new ResolvedProduct(existing.Id, definition, IsNew: false));
            productIds[name] = existing.Id;
            ReportReuse("Product", name, path, ProductDifferences(definition, existing), Add);
        }

        // ── Recipes and their ingredients ───────────────────────────────────
        var referencedRecipes = days
            .SelectMany(d => d.Meals ?? [])
            .Where(m => !string.IsNullOrWhiteSpace(m.Recipe))
            .Select(m => m.Recipe);
        var existingRecipes = Group(
            await recipeRepository.GetByNamesAsync(
                [.. recipeDefs.Keys.Concat(referencedRecipes).Distinct(StringComparer.OrdinalIgnoreCase)], userId, ct),
            r => r.Name);

        var recipeIds = new Dictionary<string, RecipeId>(StringComparer.OrdinalIgnoreCase);
        var resolvedRecipes = new List<ResolvedRecipe>();

        foreach (var (name, (definition, index)) in recipeDefs)
        {
            var path = $"recipes[{index}].name";
            var existing = PickExisting(existingRecipes, name, path, "Recipe", fallback, Add);
            // A reused recipe keeps its stored ingredients; the file's lines only feed the difference report,
            // so legacy mode must not fail the import over them.
            AddIssue addIngredientIssue = existing is not null && !strict
                ? (severity, category, p, item, message, resolution) => Add(severity == Error ? Warning : severity, category, p, item, message, resolution)
                : Add;
            var ingredients = ResolveIngredients(definition, index, productIds, existingProducts, strict, fallback, addIngredientIssue);

            if (existing is null)
            {
                var id = RecipeId.New();
                resolvedRecipes.Add(new ResolvedRecipe(id, definition, IsNew: true, ingredients, [.. ingredients.Select(i => i.ProductId)]));
                recipeIds[name] = id;
                continue;
            }

            resolvedRecipes.Add(new ResolvedRecipe(existing.Id, definition, IsNew: false, ingredients, [.. existing.Ingredients.Select(i => i.ProductId)]));
            recipeIds[name] = existing.Id;
            ReportReuse("Recipe", name, path, RecipeDifferences(definition, ingredients, existing), Add);
        }

        // ── Schedule: dates, slots, recipes ─────────────────────────────────
        var schedule = await scheduleRepository.GetByPersonIdAsync(personId, ct);
        var slotNames = schedule is null
            ? DefaultMealSlots.Select(s => s.Name).ToList()
            : schedule.Slots.OrderBy(s => s.SortOrder).Select(s => s.Name).ToList();
        var slotList = string.Join(", ", slotNames);
        var createsOtherSlot = false;

        if (schedule is null && days.Any(d => (d.Meals ?? []).Count > 0))
            Add(fallback, "referential", "schedule", null,
                strict
                    ? "You have no meal schedule yet."
                    : $"You have no meal schedule yet; a default one ({slotList}) will be created.",
                strict ? "Set up your meal slots before importing." : null);

        var meals = new List<(ResolvedMeal Meal, string Path)>();

        for (var i = 0; i < days.Count; i++)
        {
            var day = days[i];
            var path = $"schedule[{i}]";

            if (!DateOnly.TryParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                Add(Error, "validation", $"{path}.date", null, $"'{day.Date}' is not a valid date (expected YYYY-MM-DD).", "Use ISO 8601 date format.");
                continue;
            }

            var dayMeals = day.Meals ?? [];
            for (var j = 0; j < dayMeals.Count; j++)
            {
                var meal = dayMeals[j];
                var mealPath = $"{path}.meals[{j}]";

                if (meal.Servings is <= 0)
                    Add(Error, "validation", $"{mealPath}.servings", meal.Recipe, $"Servings of the meal on {day.Date} must be positive.", null);

                var slotName = slotNames.FirstOrDefault(n => string.Equals(n, meal.Type, StringComparison.OrdinalIgnoreCase));
                if (slotName is null)
                {
                    Add(fallback, "referential", $"{mealPath}.type", meal.Type,
                        strict
                            ? $"Meal type '{meal.Type}' on {day.Date} does not match any of your meal slots."
                            : $"Meal type '{meal.Type}' on {day.Date} does not match any of your meal slots; the meal will be placed in '{OtherSlot}'.",
                        $"Use one of: {slotList}.");
                    slotName = OtherSlot;
                    createsOtherSlot |= !slotNames.Contains(OtherSlot, StringComparer.OrdinalIgnoreCase);
                }

                RecipeId? recipeId = null;
                if (string.IsNullOrWhiteSpace(meal.Recipe))
                    Add(Error, "referential", $"{mealPath}.recipe", null, $"Meal {j} on {day.Date} has no recipe name.", "Specify the recipe name.");
                else
                    recipeId = ResolveReference(recipeIds, existingRecipes, meal.Recipe, $"{mealPath}.recipe", "Recipe", fallback, r => r.Id, Add);

                if (recipeId is not null && meal.Servings is not <= 0)
                {
                    var recipeName = meal.Recipe;
                    if (recipeDefs.TryGetValue(meal.Recipe, out var defined))
                        recipeName = defined.Definition.Name;
                    else if (existingRecipes.TryGetValue(meal.Recipe, out var stored))
                        recipeName = stored[0].Name;
                    meals.Add((new ResolvedMeal(date, slotName, recipeId, recipeName, meal.Servings ?? 1m, meal.Notes), mealPath));
                }
            }
        }

        if (createsOtherSlot && slotNames.Count >= MaxSlots)
            Add(Error, "referential", "schedule", OtherSlot,
                $"Your schedule already has {MaxSlots} slots, so no '{OtherSlot}' slot can be added for unknown meal types.",
                $"Use one of: {slotList}.");

        if (meals.Count > 0)
        {
            var first = meals.Min(m => m.Meal.Date);
            var last = meals.Max(m => m.Meal.Date);

            if (strict && last.DayNumber - first.DayNumber + 1 > MaxDays)
                Add(Error, "validation", "schedule", null,
                    $"The schedule spans {first:yyyy-MM-dd} to {last:yyyy-MM-dd}; at most {MaxDays} days can be imported at once.",
                    "Split the plan into monthly imports.");

            if (schedule is not null)
                await ReportOccupiedSlotsAsync(personId, schedule, meals, first, last, strict && !appendToOccupiedSlots, Add, ct);
        }

        return new ResolvedImport(issues, resolvedProducts, resolvedRecipes, [.. meals.Select(m => m.Meal)], schedule, createsOtherSlot,
            [.. existingRecipes.Values.SelectMany(r => r)]);
    }

    private static bool ExceedsBounds(int products, int recipes, int meals, AddIssue add)
    {
        var exceeded = false;

        foreach (var (section, noun, count, max) in new[]
                 {
                     ("products", "products", products, MaxProducts),
                     ("recipes", "recipes", recipes, MaxRecipes),
                     ("schedule", "meals", meals, MaxMeals),
                 })
        {
            if (count <= max)
                continue;

            add(Error, "validation", section, null, $"The import has {count} {noun}; at most {max} are allowed.", "Split the plan into smaller imports.");
            exceeded = true;
        }

        return exceeded;
    }

    /// <summary>Validates product definitions; the last definition wins for a repeated name.</summary>
    private static Dictionary<string, (ImportProductDto Definition, int Index)> CollectProducts(
        IReadOnlyList<ImportProductDto> products, string fallback, AddIssue add)
    {
        var definitions = new Dictionary<string, (ImportProductDto Definition, int Index)>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < products.Count; i++)
        {
            var p = products[i];
            var path = $"products[{i}]";

            if (!IsValidName(p.Name, path, "Product", add))
                continue;

            foreach (var (field, value) in new[]
                     {
                         ("caloriesPer100g", p.CaloriesPer100g), ("proteinPer100g", p.ProteinPer100g),
                         ("carbsPer100g", p.CarbsPer100g), ("fatPer100g", p.FatPer100g), ("fiberPer100g", p.FiberPer100g),
                     })
            {
                if (value is < 0)
                    add(Error, "validation", $"{path}.{field}", p.Name, $"{field} of '{p.Name}' is negative ({value}).", "Use a value ≥ 0, or omit it when unknown.");
            }

            if (p.Unit is not null && !UnitConverter.IsValidUnit(p.Unit))
                add(fallback, "validation", $"{path}.unit", p.Name, $"Unit '{p.Unit}' of '{p.Name}' is not supported.", SupportedUnits());
            if (p.DensityGramsPerMl is <= 0)
                add(Error, "validation", $"{path}.densityGramsPerMl", p.Name, $"Density of '{p.Name}' must be positive.", null);
            if (p.GramPerPiece is <= 0)
                add(Error, "validation", $"{path}.gramPerPiece", p.Name, $"Grams per piece of '{p.Name}' must be positive.", null);

            if (!definitions.TryAdd(p.Name, (p, i)))
            {
                add(fallback, "conflict", $"{path}.name", p.Name, $"Product '{p.Name}' appears more than once. The last definition will be used.", "Remove duplicates.");
                definitions[p.Name] = (p, i);
            }
        }

        return definitions;
    }

    /// <summary>Validates recipe definitions; the last definition wins for a repeated name.</summary>
    private static Dictionary<string, (ImportRecipeDto Definition, int Index)> CollectRecipes(
        IReadOnlyList<ImportRecipeDto> recipes, bool strict, string fallback, AddIssue add)
    {
        var definitions = new Dictionary<string, (ImportRecipeDto Definition, int Index)>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < recipes.Count; i++)
        {
            var r = recipes[i];
            var path = $"recipes[{i}]";

            if (!IsValidName(r.Name, path, "Recipe", add))
                continue;

            if (r.Servings is <= 0)
                add(Error, "validation", $"{path}.servings", r.Name, $"Servings of '{r.Name}' must be positive.", null);
            if (r.PrepTimeMinutes is < 0)
                add(Error, "validation", $"{path}.prepTimeMinutes", r.Name, $"Preparation time of '{r.Name}' must not be negative.", null);

            var ingredientCount = (r.Ingredients ?? []).Count;
            if (ingredientCount == 0)
                add(fallback, "validation", $"{path}.ingredients", r.Name, $"Recipe '{r.Name}' has no ingredients.", "Add at least one ingredient.");
            else if (strict && ingredientCount > MaxIngredientsPerRecipe)
                add(Error, "validation", $"{path}.ingredients", r.Name,
                    $"Recipe '{r.Name}' has {ingredientCount} ingredients; at most {MaxIngredientsPerRecipe} are allowed.", null);

            if (!definitions.TryAdd(r.Name, (r, i)))
            {
                add(fallback, "conflict", $"{path}.name", r.Name, $"Recipe '{r.Name}' appears more than once. The last definition will be used.", "Remove duplicates.");
                definitions[r.Name] = (r, i);
            }
        }

        return definitions;
    }

    private static List<ResolvedIngredient> ResolveIngredients(
        ImportRecipeDto recipe,
        int index,
        Dictionary<string, ProductId> productIds,
        Dictionary<string, List<Product>> existingProducts,
        bool strict,
        string fallback,
        AddIssue add)
    {
        var resolved = new List<ResolvedIngredient>();
        var ingredients = recipe.Ingredients ?? [];

        for (var j = 0; j < ingredients.Count; j++)
        {
            var ing = ingredients[j];
            var path = $"recipes[{index}].ingredients[{j}]";

            if (string.IsNullOrWhiteSpace(ing.Product))
            {
                add(Error, "referential", $"{path}.product", recipe.Name, $"Ingredient {j} of recipe '{recipe.Name}' has no product name.", "Specify the product name.");
                continue;
            }

            if (ing.Amount is <= 0)
                add(Error, "validation", $"{path}.amount", ing.Product, $"Amount of '{ing.Product}' in '{recipe.Name}' must be positive.", null);
            else if (ing.Amount is null)
                add(fallback, "validation", $"{path}.amount", ing.Product,
                    strict
                        ? $"Amount of '{ing.Product}' in '{recipe.Name}' is missing."
                        : $"Amount of '{ing.Product}' in '{recipe.Name}' is missing; {LegacyDefaultAmount} will be assumed.",
                    "Specify the amount.");

            if (string.IsNullOrWhiteSpace(ing.Unit))
                add(Error, "validation", $"{path}.unit", ing.Product, $"Unit of '{ing.Product}' in '{recipe.Name}' is missing.", "Specify the unit.");
            else if (!UnitConverter.IsValidUnit(ing.Unit))
                add(fallback, "validation", $"{path}.unit", ing.Product, $"Unit '{ing.Unit}' of '{ing.Product}' in '{recipe.Name}' is not supported.", SupportedUnits());

            var productId = ResolveReference(productIds, existingProducts, ing.Product, $"{path}.product", "Product", fallback, p => p.Id, add);
            if (productId is not null && ing.Amount is not <= 0 && !string.IsNullOrWhiteSpace(ing.Unit))
                resolved.Add(new ResolvedIngredient(productId, ing.Amount ?? LegacyDefaultAmount, ing.Unit));
        }

        return resolved;
    }

    private static bool IsValidName(string? name, string path, string kind, AddIssue add)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            add(Error, "validation", $"{path}.name", null, $"{kind} name is required.", "Provide a non-empty name.");
            return false;
        }

        if (name.Length > MaxNameLength)
        {
            add(Error, "validation", $"{path}.name", name, $"{kind} name must not exceed {MaxNameLength} characters.", null);
            return false;
        }

        return true;
    }

    private static string SupportedUnits() => $"Use one of: {string.Join(", ", UnitConverter.GetSupportedUnits())}.";

    private static Dictionary<string, List<T>> Group<T>(IEnumerable<T> items, Func<T, string> name)
        => items
            .GroupBy(name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

    /// <summary>Picks the catalog item for <paramref name="name"/>; several matches are ambiguous and the oldest is used.</summary>
    private static T? PickExisting<T>(
        Dictionary<string, List<T>> existing, string name, string path, string kind, string fallback, AddIssue add)
        where T : class
    {
        if (!existing.TryGetValue(name, out var matches))
            return null;

        if (matches.Count > 1)
            add(fallback, "conflict", path, name,
                $"{kind} '{name}' matches {matches.Count} items in your catalog; the oldest will be used.",
                "Rename the duplicates in your catalog so the name is unique.");

        return matches[0];
    }

    /// <summary>Resolves a reference to the file's own definitions first, then to the caller's catalog.</summary>
    private static TId? ResolveReference<T, TId>(
        Dictionary<string, TId> resolved,
        Dictionary<string, List<T>> existing,
        string name,
        string path,
        string kind,
        string fallback,
        Func<T, TId> id,
        AddIssue add)
        where T : class
        where TId : class
    {
        if (resolved.TryGetValue(name, out var known))
            return known;

        var match = PickExisting(existing, name, path, kind, fallback, add);
        if (match is null)
        {
            add(Error, "referential", path, name,
                $"{kind} '{name}' is neither defined in the import nor in your catalog.",
                $"Add the {kind.ToLowerInvariant()} to the import or create it first.");
            return null;
        }

        // Cache so a name referenced many times reports its ambiguity once.
        return resolved[name] = id(match);
    }

    private static void ReportReuse(string kind, string name, string path, List<string> differences, AddIssue add)
    {
        if (differences.Count == 0)
            add(Info, "conflict", path, name, $"{kind} '{name}' already exists and will be reused.");
        else
            add(Warning, "conflict", path, name,
                $"{kind} '{name}' already exists with a different {string.Join(", ", differences)}; the existing {kind.ToLowerInvariant()} will be reused unchanged.",
                $"Rename the {kind.ToLowerInvariant()} to import it separately, or edit the existing one.");
    }

    private static List<string> ProductDifferences(ImportProductDto definition, Product existing)
    {
        var differences = new List<string>();
        var nutrition = existing.Nutrition;

        if (definition.CaloriesPer100g != nutrition.Calories) differences.Add("caloriesPer100g");
        if (definition.ProteinPer100g != nutrition.Protein) differences.Add("proteinPer100g");
        if (definition.CarbsPer100g != nutrition.Carbs) differences.Add("carbsPer100g");
        if (definition.FatPer100g != nutrition.Fat) differences.Add("fatPer100g");
        if (definition.FiberPer100g != nutrition.Fiber) differences.Add("fiberPer100g");
        if (!string.Equals(definition.Unit ?? "g", existing.DefaultUnit, StringComparison.OrdinalIgnoreCase)) differences.Add("unit");
        if (definition.DensityGramsPerMl != existing.DensityGramsPerMl) differences.Add("densityGramsPerMl");
        if (definition.GramPerPiece != existing.GramPerPiece) differences.Add("gramPerPiece");

        return differences;
    }

    private static List<string> RecipeDifferences(ImportRecipeDto definition, List<ResolvedIngredient> ingredients, Recipe existing)
    {
        var differences = new List<string>();

        if ((definition.Servings ?? 1) != existing.Servings)
            differences.Add("servings");

        static string Key(ProductId productId, decimal amount, string unit)
            => string.Create(CultureInfo.InvariantCulture, $"{productId.Value}|{amount:0.####}|{unit.ToLowerInvariant()}");

        var imported = ingredients.Select(i => Key(i.ProductId, i.Amount, i.Unit)).Order(StringComparer.Ordinal);
        var stored = existing.Ingredients.Select(i => Key(i.ProductId, i.Amount, i.Unit)).Order(StringComparer.Ordinal);
        if (!imported.SequenceEqual(stored, StringComparer.Ordinal))
            differences.Add("ingredients");

        return differences;
    }

    private async Task ReportOccupiedSlotsAsync(
        Guid personId,
        MealScheduleConfig schedule,
        List<(ResolvedMeal Meal, string Path)> meals,
        DateOnly first,
        DateOnly last,
        bool occupiedIsError,
        AddIssue add,
        CancellationToken ct)
    {
        var slotNameById = schedule.Slots.ToDictionary(s => s.Id, s => s.Name);
        var occupied = (await mealEntryRepository.GetOccupiedSlotsAsync(personId, first, last, ct))
            .Where(e => slotNameById.ContainsKey(e.MealSlotId))
            .Select(e => (e.Date, slotNameById[e.MealSlotId]))
            .ToHashSet();
        var planned = new HashSet<(DateOnly, string)>();

        foreach (var (meal, path) in meals)
        {
            // When occupied slots are an error, a slot filled twice by the file itself is one too.
            if (occupiedIsError && !planned.Add((meal.Date, meal.SlotName)))
            {
                add(Error, "conflict", $"{path}.type", meal.SlotName,
                    $"{meal.SlotName} on {meal.Date:yyyy-MM-dd} is planned more than once in this import.",
                    "Keep one meal per slot, or allow appending to occupied slots.");
                continue;
            }

            if (!occupied.Contains((meal.Date, meal.SlotName)))
                continue;

            add(occupiedIsError ? Error : Warning, "conflict", $"{path}.type", meal.SlotName,
                occupiedIsError
                    ? $"{meal.SlotName} on {meal.Date:yyyy-MM-dd} already has a planned meal."
                    : $"{meal.SlotName} on {meal.Date:yyyy-MM-dd} already has a planned meal; the imported meal will be added alongside it.",
                occupiedIsError ? "Choose another date or slot, or allow appending to occupied slots." : null);
        }
    }
}
