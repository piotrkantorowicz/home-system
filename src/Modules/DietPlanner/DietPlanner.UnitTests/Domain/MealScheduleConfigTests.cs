namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class MealScheduleConfigTests
{
    private static readonly IReadOnlyList<(string Name, TimeOnly DefaultTime)> DefaultSlots =
    [
        ("Breakfast", new TimeOnly(7, 0)),
        ("Lunch", new TimeOnly(12, 0)),
        ("Dinner", new TimeOnly(18, 0))
    ];

    private static IReadOnlyList<MealSlotUpsert> Upserts(params (MealSlotId? Id, string Name, TimeOnly Time)[] items)
        => items.Select(i => new MealSlotUpsert(i.Id, i.Name, i.Time)).ToList();

    [Fact]
    public void Create_WithValidData_CreatesConfig()
    {
        var id = MealScheduleConfigId.New();

        MealScheduleConfig config = MealScheduleConfig.Create(id, "user-1", DefaultSlots);

        config.Id.ShouldBe(id);
        config.UserId.ShouldBe("user-1");
        config.Slots.Count.ShouldBe(3);
        config.UpdatedAt.ShouldBeNull();
        config.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Create_WithValidData_SlotsHaveCorrectNamesAndTimes()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        var slots = config.Slots.OrderBy(s => s.SortOrder).ToList();
        slots[0].Name.ShouldBe("Breakfast");
        slots[0].DefaultTime.ShouldBe(new TimeOnly(7, 0));
        slots[1].Name.ShouldBe("Lunch");
        slots[2].Name.ShouldBe("Dinner");
    }

    [Fact]
    public void Create_WithEightSlots_Succeeds()
    {
        var slots = Enumerable.Range(1, 8)
            .Select(i => ($"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", slots);

        config.Slots.Count.ShouldBe(8);
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsDomainException()
    {
        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), null!, DefaultSlots);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void Create_WithZeroSlots_ThrowsDomainException()
    {
        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", []);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("at least 1 slot");
    }

    [Fact]
    public void Create_WithNineSlots_ThrowsDomainException()
    {
        var slots = Enumerable.Range(1, 9)
            .Select(i => ($"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", slots);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("more than 8 slots");
    }

    [Fact]
    public void ApplyUpdate_KeepingSameSlots_PreservesIdsAndUpdatesFields()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Brunch", new TimeOnly(10, 0)),
            (existing[1].Id, "Lunch", new TimeOnly(12, 0)),
            (existing[2].Id, "Dinner", new TimeOnly(19, 0))));

        var updated = config.Slots.OrderBy(s => s.SortOrder).ToList();
        updated[0].Id.ShouldBe(existing[0].Id);
        updated[0].Name.ShouldBe("Brunch");
        updated[0].DefaultTime.ShouldBe(new TimeOnly(10, 0));
        updated[2].DefaultTime.ShouldBe(new TimeOnly(19, 0));
    }

    [Fact]
    public void ApplyUpdate_AddingNewSlot_AssignsFreshId()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[1].Id, "Lunch", existing[1].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime),
            (null, "Snack", new TimeOnly(15, 0))));

        config.Slots.Count.ShouldBe(4);
        var newSlot = config.Slots.Single(s => s.Name == "Snack");
        newSlot.Id.ShouldNotBeNull();
        newSlot.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void ApplyUpdate_OmittingSlot_RemovesIt()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime)));

        config.Slots.Count.ShouldBe(2);
        config.Slots.Any(s => s.Id == existing[1].Id).ShouldBeFalse();
    }

    [Fact]
    public void ComputeRemovedSlots_ReturnsOnlyAbsentIds()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        var removed = config.ComputeRemovedSlots(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime)));

        removed.Count.ShouldBe(1);
        removed[0].ShouldBe(existing[1].Id);
    }

    [Fact]
    public void ApplyUpdate_WithUnknownId_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        var act = () => config.ApplyUpdate(Upserts((MealSlotId.New(), "Bogus", new TimeOnly(8, 0))));

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("Unknown meal slot");
    }

    [Fact]
    public void ApplyUpdate_SetsUpdatedAt()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        config.ApplyUpdate(Upserts((null, "Single", new TimeOnly(8, 0))));

        config.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyUpdate_WithZeroSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        var act = () => config.ApplyUpdate([]);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("at least 1 slot");
    }

    [Fact]
    public void ApplyUpdate_WithNineSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var tooMany = Enumerable.Range(1, 9)
            .Select(i => new MealSlotUpsert(null, $"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        var act = () => config.ApplyUpdate(tooMany);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("more than 8 slots");
    }
}
