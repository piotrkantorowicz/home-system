namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>MealEntry</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class MealEntryTests
{
    /// <summary>With valid data: <c>Create</c> creates meal entry.</summary>
    [Fact]
    public void Create_WithValidData_CreatesMealEntry()
    {
        var id = MealEntryId.New();
        var slotId = MealSlotId.New();
        var recipeId = RecipeId.New();
        var date = new DateOnly(2024, 1, 15);

        var entry = MealEntry.Create(id, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), date, slotId, recipeId, 1.5m, "Notes", null, null, TestClock.UtcNow);

        entry.Id.ShouldBe(id);
        entry.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        entry.Date.ShouldBe(date);
        entry.MealSlotId.ShouldBe(slotId);
        entry.RecipeId.ShouldBe(recipeId);
        entry.Servings.ShouldBe(1.5m);
        entry.Notes.ShouldBe("Notes");
    }

    /// <summary>With new values: <c>Update</c> updates entry.</summary>
    [Fact]
    public void Update_WithNewValues_UpdatesEntry()
    {
        var entry = MealEntry.Create(MealEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), new DateOnly(2024, 1, 15),
            MealSlotId.New(), RecipeId.New(), 1m, null, null, null, TestClock.UtcNow);
        var newSlotId = MealSlotId.New();
        var newRecipeId = RecipeId.New();
        var newDate = new DateOnly(2024, 1, 16);

        entry.Update(newDate, newSlotId, newRecipeId, 2m, "Updated note", null, 1);

        entry.Date.ShouldBe(newDate);
        entry.MealSlotId.ShouldBe(newSlotId);
        entry.RecipeId.ShouldBe(newRecipeId);
        entry.Servings.ShouldBe(2m);
        entry.Notes.ShouldBe("Updated note");
        entry.SequenceOrder.ShouldBe(1);
    }
}
