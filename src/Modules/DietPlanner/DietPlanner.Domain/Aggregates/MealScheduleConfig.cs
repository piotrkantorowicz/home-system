namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Domain;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class MealScheduleConfig : AggregateRoot<MealScheduleConfigId>
{
    private readonly List<MealSlot> _slots = [];

    private MealScheduleConfig() { }

    public static MealScheduleConfig Create(
        MealScheduleConfigId id,
        string userId,
        IReadOnlyList<(string Name, TimeOnly DefaultTime)> slots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ValidateSlotCount(slots.Count);

        var config = new MealScheduleConfig
        {
            Id = id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        for (var i = 0; i < slots.Count; i++)
        {
            config._slots.Add(MealSlot.Create(MealSlotId.New(), slots[i].Name, slots[i].DefaultTime, i));
        }

        return config;
    }

    public string UserId { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public IReadOnlyCollection<MealSlot> Slots => _slots.AsReadOnly();

    public void UpdateSlots(IReadOnlyList<(string Name, TimeOnly DefaultTime)> slots)
    {
        ValidateSlotCount(slots.Count);

        _slots.Clear();

        for (var i = 0; i < slots.Count; i++)
        {
            _slots.Add(MealSlot.Create(MealSlotId.New(), slots[i].Name, slots[i].DefaultTime, i));
        }

        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateSlotCount(int count)
    {
        if (count < 1)
            throw new DietPlannerDomainException("Meal schedule must have at least 1 slot.");

        if (count > 8)
            throw new DietPlannerDomainException("Meal schedule cannot have more than 8 slots.");
    }
}
