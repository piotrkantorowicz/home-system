namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class MealEntry : AggregateRoot<MealEntryId>
{
    private readonly List<MealEntryActualProduct> _actualProducts = [];

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
            CreatedAt = DateTime.UtcNow,
            Status = MealEntryStatus.Planned
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

    public MealEntryStatus Status { get; private set; }
    public RecipeId? ActualRecipeId { get; private set; }
    public IReadOnlyCollection<MealEntryActualProduct> ActualProducts => _actualProducts.AsReadOnly();

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

    public void MarkDone()
    {
        if (Status == MealEntryStatus.Modified)
            throw new DietPlannerDomainException(
                "Cannot mark a modified meal as done. Reset the override first.");

        Status = MealEntryStatus.Done;
    }

    public void ApplyOverride(
        RecipeId? actualRecipeId,
        IReadOnlyList<(ProductId ProductId, decimal Amount, string Unit)> actualProducts)
    {
        ArgumentNullException.ThrowIfNull(actualProducts);

        if (actualRecipeId is null && actualProducts.Count == 0)
            throw new DietPlannerDomainException(
                "Override must include either a replacement recipe or at least one product.");

        ActualRecipeId = actualRecipeId;
        _actualProducts.Clear();
        foreach (var (productId, amount, unit) in actualProducts)
        {
            _actualProducts.Add(MealEntryActualProduct.Create(
                MealEntryActualProductId.New(), productId, amount, unit));
        }

        Status = MealEntryStatus.Modified;
    }

    public void Reset()
    {
        ActualRecipeId = null;
        _actualProducts.Clear();
        Status = MealEntryStatus.Planned;
    }
}
