using DietPlanner.Api.Data;
using DietPlanner.Api.Domain;
using DietPlanner.Api.Features.DietPlans.Import;
using DietPlanner.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DietPlanner.Tests.Integration.Import;

[Collection("Database")]
public class ImportExecutorIntegrationTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private ImportExecutor _sut = null!;
    private const string UserId = "test-import-executor-user";

    public async Task InitializeAsync()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options);

        _sut = new ImportExecutor(_db, NullLogger<ImportExecutor>.Instance);
    }

    public async Task DisposeAsync()
    {
        // Delete MealEntries for this user
        var mealEntries = await _db.MealEntries
            .Where(me => me.UserId == UserId)
            .ToListAsync();

        _db.MealEntries.RemoveRange(mealEntries);
        await _db.SaveChangesAsync();

        // Delete RecipeIngredients for recipes owned by this user
        var recipes = await _db.Recipes
            .IgnoreQueryFilters()
            .Where(r => r.CreatedByUserId == UserId)
            .Include(r => r.Ingredients)
            .ToListAsync();

        foreach (var recipe in recipes)
        {
            _db.RecipeIngredients.RemoveRange(recipe.Ingredients);
        }

        _db.Recipes.RemoveRange(recipes);
        await _db.SaveChangesAsync();

        var products = await _db.Products
            .IgnoreQueryFilters()
            .Where(p => p.CreatedByUserId == UserId)
            .ToListAsync();

        _db.Products.RemoveRange(products);
        await _db.SaveChangesAsync();

        await _db.DisposeAsync();
    }

    // ---------------------------------------------------------------------------
    // Test 1 – Happy path: creates meal entries with recipes and products
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_HappyPath_CreatesMealEntriesWithRecipesAndProducts()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var import = BuildImport(suffix, twoProducts: true);

        // Act
        var result = await _sut.ExecuteAsync(import, UserId);

        // Assert – result
        result.Should().NotBeNull();
        result.Stats.ProductsCreated.Should().Be(2);
        result.Stats.RecipesCreated.Should().Be(1);
        result.Stats.MealEntriesCreated.Should().Be(1);

        // Assert – Products in DB (2 products expected)
        var products = await _db.Products
            .Where(p => p.CreatedByUserId == UserId && p.Name.EndsWith(suffix))
            .ToListAsync();
        products.Should().HaveCount(2);

        // Assert – Recipe in DB
        var recipes = await _db.Recipes
            .Where(r => r.CreatedByUserId == UserId && r.Name.EndsWith(suffix))
            .ToListAsync();
        recipes.Should().HaveCount(1);

        // Assert – MealEntry in DB
        var mealEntries = await _db.MealEntries
            .Where(me => me.UserId == UserId)
            .ToListAsync();
        mealEntries.Should().HaveCount(1);
    }

    // ---------------------------------------------------------------------------
    // Test 2 – Existing product is reused, not duplicated
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_ExistingProduct_ReusesItInsteadOfDuplicating()
    {
        // Arrange – pre-create a product in the DB
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var productName = $"Chicken {suffix}";

        var existingProduct = new Product
        {
            Name = productName,
            CaloriesPer100g = 165,
            ProteinPer100g = 31,
            CarbsPer100g = 0,
            FatPer100g = 3.6m,
            DefaultUnit = "g",
            CreatedByUserId = UserId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(existingProduct);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Build import that references the same product by name
        var import = new ImportDto
        {
            Products =
            [
                new ImportProductDto { Name = productName, CaloriesPer100g = 165, ProteinPer100g = 31, CarbsPer100g = 0, FatPer100g = 3.6m, Unit = "g" }
            ],
            Recipes =
            [
                new ImportRecipeDto
                {
                    Name = $"Grilled Chicken {suffix}",
                    Servings = 2,
                    Ingredients = [new ImportIngredientDto { Product = productName, Amount = 200, Unit = "g" }]
                }
            ],
            Schedule =
            [
                new ImportScheduleDto
                {
                    Date = new DateOnly(2026, 1, 1),
                    Meals = [new ImportMealDto { Type = "lunch", Recipe = $"Grilled Chicken {suffix}", Servings = 1 }]
                }
            ]
        };

        // Act
        await _sut.ExecuteAsync(import, UserId);

        // Assert – only 1 product with that name exists
        var count = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(p => p.Name == productName);

        count.Should().Be(1);
    }

    // ---------------------------------------------------------------------------
    // Test 3 – All entities are saved atomically (before/after counts)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_NewProductsAndRecipes_SavedInSingleTransaction()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var import = BuildImport(suffix);

        var productsBefore = await _db.Products.CountAsync();
        var recipesBefore = await _db.Recipes.CountAsync();
        var mealEntriesBefore = await _db.MealEntries.CountAsync();

        // Act
        var result = await _sut.ExecuteAsync(import, UserId);

        // Assert – all entities appear in the DB after the call completes
        var productsAfter = await _db.Products.CountAsync();
        var recipesAfter = await _db.Recipes.CountAsync();
        var mealEntriesAfter = await _db.MealEntries.CountAsync();

        productsAfter.Should().Be(productsBefore + 1, "one Product should have been created");
        recipesAfter.Should().Be(recipesBefore + 1, "one Recipe should have been created");
        mealEntriesAfter.Should().Be(mealEntriesBefore + 1, "one MealEntry should have been created");

        result.Stats.ProductsCreated.Should().Be(1);
        result.Stats.RecipesCreated.Should().Be(1);
        result.Stats.MealEntriesCreated.Should().Be(1);
    }

    // ---------------------------------------------------------------------------
    // Test 4 – Duplicate import reuses existing products/recipes, no orphans
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_DuplicateImport_DoesNotDuplicateProductsOrRecipes()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var import = BuildImport(suffix);

        // First import
        await _sut.ExecuteAsync(import, UserId);
        _db.ChangeTracker.Clear();

        var productCountAfterFirst = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(p => p.CreatedByUserId == UserId && p.Name.EndsWith(suffix));

        var recipeCountAfterFirst = await _db.Recipes
            .IgnoreQueryFilters()
            .CountAsync(r => r.CreatedByUserId == UserId && r.Name.EndsWith(suffix));

        // Act – second import with the same product/recipe names
        var importSecond = BuildImport(suffix);

        await _sut.ExecuteAsync(importSecond, UserId);
        _db.ChangeTracker.Clear();

        // Assert – product and recipe counts must remain the same (no duplication)
        var productCountAfterSecond = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(p => p.CreatedByUserId == UserId && p.Name.EndsWith(suffix));

        var recipeCountAfterSecond = await _db.Recipes
            .IgnoreQueryFilters()
            .CountAsync(r => r.CreatedByUserId == UserId && r.Name.EndsWith(suffix));

        productCountAfterSecond.Should().Be(productCountAfterFirst,
            "the second import should reuse the existing product, not create a duplicate");

        recipeCountAfterSecond.Should().Be(recipeCountAfterFirst,
            "the second import should reuse the existing recipe, not create a duplicate");
    }

    // ---------------------------------------------------------------------------
    // Test 5 – MealEntry is correctly linked to the recipe and user
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_CreatesCorrectMealEntries_LinkedToRecipeAndUser()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var productName = $"Chicken {suffix}";
        var recipeName = $"Grilled Chicken {suffix}";

        var import = new ImportDto
        {
            Products =
            [
                new ImportProductDto
                {
                    Name = productName,
                    CaloriesPer100g = 165,
                    ProteinPer100g = 31,
                    CarbsPer100g = 0,
                    FatPer100g = 3.6m,
                    Unit = "g"
                }
            ],
            Recipes =
            [
                new ImportRecipeDto
                {
                    Name = recipeName,
                    Servings = 2,
                    Ingredients = [new ImportIngredientDto { Product = productName, Amount = 200, Unit = "g" }]
                }
            ],
            Schedule =
            [
                new ImportScheduleDto
                {
                    Date = new DateOnly(2026, 1, 1),
                    Meals =
                    [
                        new ImportMealDto { Type = "lunch", Recipe = recipeName, Servings = 1 },
                        new ImportMealDto { Type = "dinner", Recipe = recipeName, Servings = 2 }
                    ]
                }
            ]
        };

        // Act
        var result = await _sut.ExecuteAsync(import, UserId);

        // Assert – MealEntries in DB
        var mealEntries = await _db.MealEntries
            .Include(me => me.Recipe)
            .Where(me => me.UserId == UserId)
            .ToListAsync();

        mealEntries.Should().HaveCount(2, "two meals were scheduled");

        var lunch = mealEntries.Single(me => me.MealType == "lunch");
        lunch.Date.Should().Be(new DateOnly(2026, 1, 1));
        lunch.Servings.Should().Be(1);
        lunch.Recipe.Should().NotBeNull();
        lunch.Recipe.Name.Should().Be(recipeName);

        var dinner = mealEntries.Single(me => me.MealType == "dinner");
        dinner.Date.Should().Be(new DateOnly(2026, 1, 1));
        dinner.Servings.Should().Be(2);
        dinner.UserId.Should().Be(UserId);

        // Verify the recipe has the ingredient linking back to the product
        var recipe = await _db.Recipes
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Product)
            .FirstAsync(r => r.Name == recipeName);

        recipe.Ingredients.Should().HaveCount(1);
        recipe.Ingredients.First().Product.Name.Should().Be(productName);
        recipe.Ingredients.First().Amount.Should().Be(200);

        result.Stats.MealEntriesCreated.Should().Be(2);
    }

    // ---------------------------------------------------------------------------
    // Test 6 – Full month import: many products, recipes, and meal entries
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ExecuteAsync_FullMonthImport_CreatesAllEntitiesCorrectly()
    {
        // Arrange – 10 products, 7 recipes, 31 days × 3 meals = 93 meal entries
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var products = new List<ImportProductDto>
        {
            new() { Name = $"Chicken Breast {suffix}",  CaloriesPer100g = 165, ProteinPer100g = 31,   CarbsPer100g = 0,    FatPer100g = 3.6m,  Unit = "g" },
            new() { Name = $"Brown Rice {suffix}",      CaloriesPer100g = 130, ProteinPer100g = 2.7m, CarbsPer100g = 28,   FatPer100g = 0.3m,  Unit = "g" },
            new() { Name = $"Broccoli {suffix}",        CaloriesPer100g = 34,  ProteinPer100g = 2.8m, CarbsPer100g = 6.6m, FatPer100g = 0.4m,  Unit = "g" },
            new() { Name = $"Eggs {suffix}",            CaloriesPer100g = 155, ProteinPer100g = 13,   CarbsPer100g = 1.1m, FatPer100g = 11,    Unit = "piece", GramPerPiece = 60 },
            new() { Name = $"Oats {suffix}",            CaloriesPer100g = 389, ProteinPer100g = 17,   CarbsPer100g = 66,   FatPer100g = 7,     Unit = "g" },
            new() { Name = $"Salmon {suffix}",          CaloriesPer100g = 208, ProteinPer100g = 20,   CarbsPer100g = 0,    FatPer100g = 13,    Unit = "g" },
            new() { Name = $"Sweet Potato {suffix}",    CaloriesPer100g = 86,  ProteinPer100g = 1.6m, CarbsPer100g = 20,   FatPer100g = 0.1m,  Unit = "g" },
            new() { Name = $"Greek Yogurt {suffix}",    CaloriesPer100g = 97,  ProteinPer100g = 9,    CarbsPer100g = 3.6m, FatPer100g = 5,     Unit = "g" },
            new() { Name = $"Olive Oil {suffix}",       CaloriesPer100g = 884, ProteinPer100g = 0,    CarbsPer100g = 0,    FatPer100g = 100,   Unit = "ml", DensityGramsPerMl = 0.92m },
            new() { Name = $"Cottage Cheese {suffix}",  CaloriesPer100g = 98,  ProteinPer100g = 11,   CarbsPer100g = 3.4m, FatPer100g = 4.3m,  Unit = "g" },
        };

        var recipes = new List<ImportRecipeDto>
        {
            new()
            {
                Name = $"Grilled Chicken with Rice {suffix}", Servings = 2,
                Ingredients =
                [
                    new() { Product = $"Chicken Breast {suffix}", Amount = 300, Unit = "g" },
                    new() { Product = $"Brown Rice {suffix}",     Amount = 150, Unit = "g" },
                    new() { Product = $"Olive Oil {suffix}",      Amount = 10,  Unit = "ml" },
                ]
            },
            new()
            {
                Name = $"Scrambled Eggs with Broccoli {suffix}", Servings = 1,
                Ingredients =
                [
                    new() { Product = $"Eggs {suffix}",    Amount = 3, Unit = "piece" },
                    new() { Product = $"Broccoli {suffix}", Amount = 200, Unit = "g" },
                ]
            },
            new()
            {
                Name = $"Overnight Oats {suffix}", Servings = 1,
                Ingredients =
                [
                    new() { Product = $"Oats {suffix}",        Amount = 80,  Unit = "g" },
                    new() { Product = $"Greek Yogurt {suffix}", Amount = 150, Unit = "g" },
                ]
            },
            new()
            {
                Name = $"Baked Salmon with Sweet Potato {suffix}", Servings = 2,
                Ingredients =
                [
                    new() { Product = $"Salmon {suffix}",       Amount = 400, Unit = "g" },
                    new() { Product = $"Sweet Potato {suffix}",  Amount = 300, Unit = "g" },
                    new() { Product = $"Olive Oil {suffix}",     Amount = 15,  Unit = "ml" },
                ]
            },
            new()
            {
                Name = $"Cottage Cheese Bowl {suffix}", Servings = 1,
                Ingredients =
                [
                    new() { Product = $"Cottage Cheese {suffix}", Amount = 200, Unit = "g" },
                    new() { Product = $"Oats {suffix}",           Amount = 40,  Unit = "g" },
                ]
            },
            new()
            {
                Name = $"Chicken and Broccoli Stir Fry {suffix}", Servings = 2,
                Ingredients =
                [
                    new() { Product = $"Chicken Breast {suffix}", Amount = 250, Unit = "g" },
                    new() { Product = $"Broccoli {suffix}",       Amount = 300, Unit = "g" },
                    new() { Product = $"Olive Oil {suffix}",      Amount = 20,  Unit = "ml" },
                ]
            },
            new()
            {
                Name = $"Salmon Rice Bowl {suffix}", Servings = 2,
                Ingredients =
                [
                    new() { Product = $"Salmon {suffix}",     Amount = 300, Unit = "g" },
                    new() { Product = $"Brown Rice {suffix}", Amount = 200, Unit = "g" },
                ]
            },
        };

        // 31 days, 3 meals/day, rotating through recipes
        var startDate = new DateOnly(2026, 3, 1);
        var recipeNames = recipes.Select(r => r.Name).ToList();

        var schedule = new List<ImportScheduleDto>();
        for (var day = 0; day < 31; day++)
        {
            var date = startDate.AddDays(day);
            schedule.Add(new ImportScheduleDto
            {
                Date = date,
                Meals =
                [
                    new() { Type = "breakfast", Recipe = recipeNames[(day * 3 + 0) % recipeNames.Count], Servings = 1 },
                    new() { Type = "lunch",     Recipe = recipeNames[(day * 3 + 1) % recipeNames.Count], Servings = 1 },
                    new() { Type = "dinner",    Recipe = recipeNames[(day * 3 + 2) % recipeNames.Count], Servings = 1 },
                ]
            });
        }

        var import = new ImportDto
        {
            Products  = products,
            Recipes   = recipes,
            Schedule  = schedule,
        };

        // Act
        var result = await _sut.ExecuteAsync(import, UserId);

        // Assert – top-level result
        result.Should().NotBeNull();

        // Assert – stats
        result.Stats.ProductsCreated.Should().Be(10);
        result.Stats.RecipesCreated.Should().Be(7);
        result.Stats.MealEntriesCreated.Should().Be(93); // 31 days × 3 meals

        // Assert – DB counts
        var dbProducts = await _db.Products
            .Where(p => p.CreatedByUserId == UserId && p.Name.EndsWith(suffix))
            .ToListAsync();
        dbProducts.Should().HaveCount(10);

        var dbRecipes = await _db.Recipes
            .Include(r => r.Ingredients)
            .Where(r => r.CreatedByUserId == UserId && r.Name.EndsWith(suffix))
            .ToListAsync();
        dbRecipes.Should().HaveCount(7);
        dbRecipes.SelectMany(r => r.Ingredients).Should().NotBeEmpty();

        var dbMealEntries = await _db.MealEntries
            .Where(me => me.UserId == UserId)
            .ToListAsync();
        dbMealEntries.Should().HaveCount(93);

        // Assert – every day has exactly 3 meals
        var byDate = dbMealEntries.GroupBy(me => me.Date).ToList();
        byDate.Should().HaveCount(31, "31 days scheduled");
        byDate.Should().AllSatisfy(g => g.Should().HaveCount(3, $"day {g.Key} should have 3 meals"));

        // Assert – all 3 meal types appear across the plan
        dbMealEntries.Select(me => me.MealType).Distinct()
            .Should().BeEquivalentTo(["breakfast", "lunch", "dinner"]);
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static ImportDto BuildImport(string suffix = "", bool twoProducts = false)
    {
        var products = new List<ImportProductDto>
        {
            new ImportProductDto { Name = $"Chicken {suffix}", CaloriesPer100g = 165, ProteinPer100g = 31, CarbsPer100g = 0, FatPer100g = 3.6m, Unit = "g" }
        };

        var ingredients = new List<ImportIngredientDto>
        {
            new ImportIngredientDto { Product = $"Chicken {suffix}", Amount = 200, Unit = "g" }
        };

        if (twoProducts)
        {
            products.Add(new ImportProductDto { Name = $"Rice {suffix}", CaloriesPer100g = 130, ProteinPer100g = 2.7m, CarbsPer100g = 28, FatPer100g = 0.3m, Unit = "g" });
            ingredients.Add(new ImportIngredientDto { Product = $"Rice {suffix}", Amount = 100, Unit = "g" });
        }

        return new ImportDto
        {
            Products = products,
            Recipes =
            [
                new ImportRecipeDto
                {
                    Name = $"Grilled Chicken {suffix}",
                    Servings = 2,
                    Ingredients = ingredients
                }
            ],
            Schedule =
            [
                new ImportScheduleDto
                {
                    Date = new DateOnly(2026, 1, 1),
                    Meals = [new ImportMealDto { Type = "lunch", Recipe = $"Grilled Chicken {suffix}", Servings = 1 }]
                }
            ]
        };
    }
}
