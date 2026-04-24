namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.ValueObjects;

public sealed class MealEntry : AggregateRoot<MealEntryId>
{
    private MealEntry() { }

    public static MealEntry Create(
        MealEntryId id,
        string userId,
        DateOnly date,
        MealSlotId mealSlotId,
        RecipeId recipeId,
        decimal servings,
        string? notes,
        TimeOnly? mealTime,
        int? sequenceOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(mealSlotId);
        ArgumentNullException.ThrowIfNull(recipeId);

        return new MealEntry
        {
            Id = id,
            UserId = userId,
            Date = date,
            MealSlotId = mealSlotId,
            RecipeId = recipeId,
            Servings = servings,
            Notes = notes,
            MealTime = mealTime,
            SequenceOrder = sequenceOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string UserId { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public MealSlotId MealSlotId { get; private set; } = default!;
    public RecipeId RecipeId { get; private set; } = default!;
    public decimal Servings { get; private set; } = 1m;
    public string? Notes { get; private set; }
    public TimeOnly? MealTime { get; private set; }
    public int? SequenceOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void Update(
        DateOnly date,
        MealSlotId mealSlotId,
        RecipeId recipeId,
        decimal servings,
        string? notes,
        TimeOnly? mealTime,
        int? sequenceOrder)
    {
        ArgumentNullException.ThrowIfNull(mealSlotId);
        ArgumentNullException.ThrowIfNull(recipeId);

        Date = date;
        MealSlotId = mealSlotId;
        RecipeId = recipeId;
        Servings = servings;
        Notes = notes;
        MealTime = mealTime;
        SequenceOrder = sequenceOrder;
    }
}
