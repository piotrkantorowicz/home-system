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
        string mealType,
        RecipeId recipeId,
        decimal servings,
        string? notes,
        TimeOnly? mealTime,
        int? sequenceOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(mealType);

        return new MealEntry
        {
            Id = id,
            UserId = userId,
            Date = date,
            MealType = mealType,
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
    public string MealType { get; private set; } = default!;
    public RecipeId RecipeId { get; private set; } = default!;
    public decimal Servings { get; private set; } = 1m;
    public string? Notes { get; private set; }
    public TimeOnly? MealTime { get; private set; }
    public int? SequenceOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void Update(
        DateOnly date,
        string mealType,
        RecipeId recipeId,
        decimal servings,
        string? notes,
        TimeOnly? mealTime,
        int? sequenceOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mealType);

        Date = date;
        MealType = mealType;
        RecipeId = recipeId;
        Servings = servings;
        Notes = notes;
        MealTime = mealTime;
        SequenceOrder = sequenceOrder;
    }
}
