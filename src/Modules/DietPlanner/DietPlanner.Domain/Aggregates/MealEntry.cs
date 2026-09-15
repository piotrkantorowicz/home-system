namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One planned meal on a user's calendar: a recipe, a number of servings, a date and the meal slot
/// it belongs to. Completing it records what was actually eaten — either as planned
/// (<see cref="MarkDone"/>) or with substitutions (<see cref="ApplyOverride"/>), which drive the
/// day's real macro totals and the reminder / missed-meal notifications.
/// </summary>
public sealed class MealEntry : AggregateRoot<MealEntryId>
{
    private readonly List<MealEntryActualProduct> _actualProducts = [];

    private MealEntry() { }

    /// <summary>Plans a meal; it starts in <see cref="MealEntryStatus.Planned"/>.</summary>
    /// <param name="id">Identifier for the new entry.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="date">Calendar day the meal is planned for.</param>
    /// <param name="mealSlotId">Slot from the user's meal schedule (breakfast, lunch, …).</param>
    /// <param name="recipeId">The recipe planned to be eaten.</param>
    /// <param name="servings">How many servings of the recipe.</param>
    /// <param name="notes">Optional free-text note.</param>
    /// <param name="mealTime">Optional time overriding the slot's default; reminders use it when set.</param>
    /// <param name="sequenceOrder">Optional ordering among entries in the same slot.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="mealSlotId"/> or <paramref name="recipeId"/> is null.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static MealEntry Create(
        MealEntryId id,
        string userId,
        DateOnly date,
        MealSlotId mealSlotId,
        RecipeId recipeId,
        decimal servings,
        string? notes,
        TimeOnly? mealTime,
        int? sequenceOrder,
        DateTime now)
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
            CreatedAt = now,
            Status = MealEntryStatus.Planned
        };
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Calendar day the meal is planned for.</summary>
    public DateOnly Date { get; private set; }
    /// <summary>Slot from the user's meal schedule.</summary>
    public MealSlotId MealSlotId { get; private set; } = default!;
    /// <summary>The recipe as planned; see <see cref="ActualRecipeId"/> for what was eaten instead.</summary>
    public RecipeId RecipeId { get; private set; } = default!;
    /// <summary>Servings of the planned recipe.</summary>
    public decimal Servings { get; private set; } = 1m;
    /// <summary>Optional free-text note.</summary>
    public string? Notes { get; private set; }
    /// <summary>Time overriding the slot's default; <see langword="null"/> means the slot time applies.</summary>
    public TimeOnly? MealTime { get; private set; }
    /// <summary>Ordering among entries in the same slot; <see langword="null"/> sorts last.</summary>
    public int? SequenceOrder { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Where the entry is in its lifecycle.</summary>
    public MealEntryStatus Status { get; private set; }
    /// <summary>Replacement recipe recorded by <see cref="ApplyOverride"/>, if any.</summary>
    public RecipeId? ActualRecipeId { get; private set; }
    /// <summary>Products actually eaten, recorded by <see cref="ApplyOverride"/>; empty unless the entry is <see cref="MealEntryStatus.Modified"/>.</summary>
    public IReadOnlyCollection<MealEntryActualProduct> ActualProducts => _actualProducts.AsReadOnly();

    /// <summary>Re-plans the entry; does not touch completion state.</summary>
    /// <param name="date">New calendar day.</param>
    /// <param name="mealSlotId">New slot.</param>
    /// <param name="recipeId">New planned recipe.</param>
    /// <param name="servings">New number of servings.</param>
    /// <param name="notes">New note, or <see langword="null"/> to clear it.</param>
    /// <param name="mealTime">New time override, or <see langword="null"/> to fall back to the slot time.</param>
    /// <param name="sequenceOrder">New ordering, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mealSlotId"/> or <paramref name="recipeId"/> is null.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
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

    /// <summary>Records that the meal was eaten exactly as planned.</summary>
    /// <exception cref="DietPlannerDomainException">The entry has an override; call <see cref="Reset"/> first.</exception>
    public void MarkDone()
    {
        if (Status == MealEntryStatus.Modified)
            throw new DietPlannerDomainException(
                "Cannot mark a modified meal as done. Reset the override first.");

        Status = MealEntryStatus.Done;
    }

    /// <summary>
    /// Records that something other than the plan was eaten and moves the entry to
    /// <see cref="MealEntryStatus.Modified"/>. Replaces any previous override.
    /// </summary>
    /// <param name="actualRecipeId">Recipe eaten instead of the planned one, or <see langword="null"/>.</param>
    /// <param name="actualProducts">Individual products eaten (product, amount, unit); may be empty when a recipe is given.</param>
    /// <exception cref="ArgumentNullException"><paramref name="actualProducts"/> is null.</exception>
    /// <exception cref="DietPlannerDomainException">Neither a recipe nor any product was supplied, or a product line is invalid.</exception>
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

    /// <summary>Discards completion data and returns the entry to <see cref="MealEntryStatus.Planned"/>.</summary>
    public void Reset()
    {
        ActualRecipeId = null;
        _actualProducts.Clear();
        Status = MealEntryStatus.Planned;
    }
}
