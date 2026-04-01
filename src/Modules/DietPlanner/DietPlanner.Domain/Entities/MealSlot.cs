namespace DietPlanner.Domain.Entities;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.ValueObjects;

public sealed class MealSlot : Entity<MealSlotId>
{
    private MealSlot() { }

    internal static MealSlot Create(MealSlotId id, string name, TimeOnly defaultTime, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MealSlot { Id = id, Name = name, DefaultTime = defaultTime, SortOrder = sortOrder };
    }

    public string Name { get; private set; } = default!;
    public TimeOnly DefaultTime { get; private set; }
    public int SortOrder { get; private set; }

    internal void Update(string name, TimeOnly defaultTime, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        DefaultTime = defaultTime;
        SortOrder = sortOrder;
    }
}
