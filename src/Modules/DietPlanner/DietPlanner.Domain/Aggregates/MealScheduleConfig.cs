namespace DietPlanner.Domain.Aggregates;

using Shared.Abstractions.Core.Domain;
using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed record MealSlotUpsert(MealSlotId? Id, string Name, TimeOnly DefaultTime);

public sealed class MealScheduleConfig : AggregateRoot<MealScheduleConfigId>
{
    private readonly List<MealSlot> _slots = [];

    private MealScheduleConfig() { }

    public static MealScheduleConfig Create(
        MealScheduleConfigId id,
        string userId,
        IReadOnlyList<(string Name, TimeOnly DefaultTime)> slots)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DietPlannerDomainException("User ID is required.");

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

    /// <summary>
    /// Returns the slot ids that would be removed by applying <paramref name="upserts"/>.
    /// Callers should use this to verify no MealEntry references those slots before calling
    /// <see cref="ApplyUpdate"/>.
    /// </summary>
    public IReadOnlyList<MealSlotId> ComputeRemovedSlots(IReadOnlyList<MealSlotUpsert> upserts)
    {
        var keptIds = upserts
            .Where(u => u.Id is not null)
            .Select(u => u.Id!)
            .ToHashSet();

        return _slots
            .Where(s => !keptIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToList();
    }

    /// <summary>
    /// Applies the diff: updates kept slots in place (preserving id), adds new slots,
    /// removes slots that are absent from the upsert list.
    /// </summary>
    public void ApplyUpdate(IReadOnlyList<MealSlotUpsert> upserts)
    {
        ValidateSlotCount(upserts.Count);

        var keptIds = upserts
            .Where(u => u.Id is not null)
            .Select(u => u.Id!)
            .ToHashSet();

        // Validate: every supplied id must reference an existing slot
        foreach (var u in upserts)
        {
            if (u.Id is not null && _slots.All(s => s.Id != u.Id))
                throw new DietPlannerDomainException($"Unknown meal slot id '{u.Id.Value}'.");
        }

        _slots.RemoveAll(s => !keptIds.Contains(s.Id));

        for (var i = 0; i < upserts.Count; i++)
        {
            var upsert = upserts[i];
            if (upsert.Id is null)
            {
                _slots.Add(MealSlot.Create(MealSlotId.New(), upsert.Name, upsert.DefaultTime, i));
            }
            else
            {
                var existing = _slots.First(s => s.Id == upsert.Id);
                existing.Update(upsert.Name, upsert.DefaultTime, i);
            }
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
