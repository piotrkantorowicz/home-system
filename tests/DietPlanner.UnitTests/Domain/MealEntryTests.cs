namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public sealed class MealEntryTests
{
    [Fact]
    public void Create_WithValidData_CreatesMealEntry()
    {
        var id = MealEntryId.New();
        var recipeId = RecipeId.New();
        var date = new DateOnly(2024, 1, 15);

        var entry = MealEntry.Create(id, "user-1", date, "Breakfast", recipeId, 1.5m, "Notes", null, null);

        entry.Id.ShouldBe(id);
        entry.UserId.ShouldBe("user-1");
        entry.Date.ShouldBe(date);
        entry.MealType.ShouldBe("Breakfast");
        entry.RecipeId.ShouldBe(recipeId);
        entry.Servings.ShouldBe(1.5m);
        entry.Notes.ShouldBe("Notes");
    }

    [Fact]
    public void Update_WithNewValues_UpdatesEntry()
    {
        var entry = MealEntry.Create(MealEntryId.New(), "user-1", new DateOnly(2024, 1, 15),
            "Breakfast", RecipeId.New(), 1m, null, null, null);
        var newRecipeId = RecipeId.New();
        var newDate = new DateOnly(2024, 1, 16);

        entry.Update(newDate, "Lunch", newRecipeId, 2m, "Updated note", null, 1);

        entry.Date.ShouldBe(newDate);
        entry.MealType.ShouldBe("Lunch");
        entry.RecipeId.ShouldBe(newRecipeId);
        entry.Servings.ShouldBe(2m);
        entry.Notes.ShouldBe("Updated note");
        entry.SequenceOrder.ShouldBe(1);
    }
}
