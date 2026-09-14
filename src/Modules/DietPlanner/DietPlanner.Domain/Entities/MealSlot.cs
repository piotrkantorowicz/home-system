namespace DietPlanner.Domain.Entities;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One named position in a user's <see cref="Aggregates.MealScheduleConfig"/> (e.g. "Breakfast at
/// 07:00"). Meal entries attach to a slot; its <see cref="DefaultTime"/> drives reminders for
/// entries without their own time. Created and updated only through the config aggregate.
/// </summary>
public sealed class MealSlot : Entity<MealSlotId>
{
    private MealSlot() { }

    internal static MealSlot Create(MealSlotId id, string name, TimeOnly defaultTime, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DietPlannerDomainException("Slot name is required.");

        return new MealSlot { Id = id, Name = name, DefaultTime = defaultTime, SortOrder = sortOrder };
    }

    /// <summary>Display name; never blank.</summary>
    public string Name { get; private set; } = default!;
    /// <summary>Time of day meals in this slot default to.</summary>
    public TimeOnly DefaultTime { get; private set; }
    /// <summary>Zero-based display position within the schedule.</summary>
    public int SortOrder { get; private set; }

    internal void Update(string name, TimeOnly defaultTime, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DietPlannerDomainException("Slot name is required.");

        Name = name;
        DefaultTime = defaultTime;
        SortOrder = sortOrder;
    }
}
