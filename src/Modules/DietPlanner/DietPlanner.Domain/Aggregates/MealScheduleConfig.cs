namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Entities;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One desired slot in a schedule update. With an <paramref name="Id"/> it edits an existing slot in
/// place; without one it creates a new slot. Slots absent from the upsert list are removed.
/// </summary>
/// <param name="Id">Identifier of the slot to keep and update, or <see langword="null"/> for a new slot.</param>
/// <param name="Name">Display name, e.g. "Breakfast"; required.</param>
/// <param name="DefaultTime">Time of day the slot's meals default to.</param>
public sealed record MealSlotUpsert(MealSlotId? Id, string Name, TimeOnly DefaultTime);

/// <summary>
/// A user's daily meal structure: between 1 and 8 ordered <see cref="MealSlot"/>s, each with a
/// default time. One config per user. Meal entries reference slots by id, so removing a slot is
/// only allowed when nothing references it — see <see cref="ComputeRemovedSlots"/>.
/// </summary>
public sealed class MealScheduleConfig : AggregateRoot<MealScheduleConfigId>
{
    private readonly List<MealSlot> _slots = [];

    private MealScheduleConfig() { }

    /// <summary>Creates a user's schedule with brand-new slots in the given order.</summary>
    /// <param name="id">Identifier for the new config.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="slots">Name and default time of each slot, in display order; 1 to 8 entries.</param>
    /// <exception cref="DietPlannerDomainException">The user id is blank, the slot count is out of range, or a slot name is blank.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static MealScheduleConfig Create(
        MealScheduleConfigId id,
        string userId,
        IReadOnlyList<(string Name, TimeOnly DefaultTime)> slots,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DietPlannerDomainException("User ID is required.");

        ValidateSlotCount(slots.Count);

        var config = new MealScheduleConfig
        {
            Id = id,
            UserId = userId,
            CreatedAt = now
        };

        for (var i = 0; i < slots.Count; i++)
        {
            config._slots.Add(MealSlot.Create(MealSlotId.New(), slots[i].Name, slots[i].DefaultTime, i));
        }

        return config;
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last <see cref="ApplyUpdate"/>, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }
    /// <summary>The slots; order by <see cref="MealSlot.SortOrder"/> for display.</summary>
    public IReadOnlyCollection<MealSlot> Slots => _slots.AsReadOnly();

    /// <summary>
    /// Returns the slot ids that would be removed by applying <paramref name="upserts"/>.
    /// Callers should use this to verify no MealEntry references those slots before calling
    /// <see cref="ApplyUpdate"/>.
    /// </summary>
    /// <param name="upserts">The desired slot list.</param>
    /// <returns>Ids of current slots that are absent from <paramref name="upserts"/>.</returns>
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
    /// <param name="upserts">The desired slot list, in display order; 1 to 8 entries.</param>
    /// <exception cref="DietPlannerDomainException">The slot count is out of range, an id does not belong to this schedule, or a slot name is blank.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void ApplyUpdate(IReadOnlyList<MealSlotUpsert> upserts, DateTime now)
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

        UpdatedAt = now;
    }

    private static void ValidateSlotCount(int count)
    {
        if (count < 1)
            throw new DietPlannerDomainException("Meal schedule must have at least 1 slot.");

        if (count > 8)
            throw new DietPlannerDomainException("Meal schedule cannot have more than 8 slots.");
    }
}
