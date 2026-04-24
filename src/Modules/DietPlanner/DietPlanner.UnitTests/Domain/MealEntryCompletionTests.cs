namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class MealEntryCompletionTests
{
    private static MealEntry NewEntry()
        => MealEntry.Create(
            MealEntryId.New(), "user-1", new DateOnly(2026, 1, 15),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null);

    [Fact]
    public void Create_DefaultsStatusToPlanned()
    {
        var entry = NewEntry();

        entry.Status.ShouldBe(MealEntryStatus.Planned);
        entry.ActualRecipeId.ShouldBeNull();
        entry.ActualProducts.ShouldBeEmpty();
    }

    [Fact]
    public void MarkDone_FromPlanned_SetsStatusToDone()
    {
        var entry = NewEntry();

        entry.MarkDone();

        entry.Status.ShouldBe(MealEntryStatus.Done);
    }

    [Fact]
    public void MarkDone_AlreadyDone_IsIdempotent()
    {
        var entry = NewEntry();
        entry.MarkDone();

        entry.MarkDone();

        entry.Status.ShouldBe(MealEntryStatus.Done);
    }

    [Fact]
    public void MarkDone_WhenModified_ThrowsDomainException()
    {
        var entry = NewEntry();
        entry.ApplyOverride(RecipeId.New(), []);

        var act = () => entry.MarkDone();

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void ApplyOverride_WithRecipeOnly_SetsActualRecipeAndStatus()
    {
        var entry = NewEntry();
        var actualRecipe = RecipeId.New();

        entry.ApplyOverride(actualRecipe, []);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
        entry.ActualRecipeId.ShouldBe(actualRecipe);
        entry.ActualProducts.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyOverride_WithProductsOnly_SetsActualProductsAndStatus()
    {
        var entry = NewEntry();
        var productId = ProductId.New();

        entry.ApplyOverride(null, [(productId, 100m, "g")]);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
        entry.ActualRecipeId.ShouldBeNull();
        entry.ActualProducts.Count.ShouldBe(1);
        entry.ActualProducts.Single().ProductId.ShouldBe(productId);
        entry.ActualProducts.Single().Amount.ShouldBe(100m);
        entry.ActualProducts.Single().Unit.ShouldBe("g");
    }

    [Fact]
    public void ApplyOverride_WithBothRecipeAndProducts_SetsBoth()
    {
        var entry = NewEntry();
        var actualRecipe = RecipeId.New();
        var productId = ProductId.New();

        entry.ApplyOverride(actualRecipe, [(productId, 50m, "g")]);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
        entry.ActualRecipeId.ShouldBe(actualRecipe);
        entry.ActualProducts.Count.ShouldBe(1);
    }

    [Fact]
    public void ApplyOverride_WithEmptyRecipeAndProducts_ThrowsDomainException()
    {
        var entry = NewEntry();

        var act = () => entry.ApplyOverride(null, []);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void ApplyOverride_TwiceReplacesPreviousOverride()
    {
        var entry = NewEntry();
        entry.ApplyOverride(RecipeId.New(), [(ProductId.New(), 100m, "g")]);

        var newRecipe = RecipeId.New();
        entry.ApplyOverride(newRecipe, []);

        entry.ActualRecipeId.ShouldBe(newRecipe);
        entry.ActualProducts.ShouldBeEmpty();
    }

    [Fact]
    public void ApplyOverride_OnDoneEntry_TransitionsToModified()
    {
        var entry = NewEntry();
        entry.MarkDone();

        entry.ApplyOverride(RecipeId.New(), []);

        entry.Status.ShouldBe(MealEntryStatus.Modified);
    }

    [Fact]
    public void Reset_ClearsOverrideAndStatus()
    {
        var entry = NewEntry();
        entry.ApplyOverride(RecipeId.New(), [(ProductId.New(), 100m, "g")]);

        entry.Reset();

        entry.Status.ShouldBe(MealEntryStatus.Planned);
        entry.ActualRecipeId.ShouldBeNull();
        entry.ActualProducts.ShouldBeEmpty();
    }

    [Fact]
    public void Reset_OnPlannedEntry_IsIdempotent()
    {
        var entry = NewEntry();

        entry.Reset();

        entry.Status.ShouldBe(MealEntryStatus.Planned);
    }
}
