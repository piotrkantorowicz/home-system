namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>MealScheduleConfig</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class MealScheduleConfigTests
{
    private static readonly IReadOnlyList<(string Name, TimeOnly DefaultTime)> DefaultSlots =
    [
        ("Breakfast", new TimeOnly(7, 0)),
        ("Lunch", new TimeOnly(12, 0)),
        ("Dinner", new TimeOnly(18, 0))
    ];

    private static List<MealSlotUpsert> Upserts(params (MealSlotId? Id, string Name, TimeOnly Time)[] items)
        => items.Select(i => new MealSlotUpsert(i.Id, i.Name, i.Time)).ToList();

    /// <summary>With valid data: <c>Create</c> creates config.</summary>
    [Fact]
    public void Create_WithValidData_CreatesConfig()
    {
        var id = MealScheduleConfigId.New();

        MealScheduleConfig config = MealScheduleConfig.Create(id, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);

        config.Id.ShouldBe(id);
        config.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        config.Slots.Count.ShouldBe(3);
        config.UpdatedAt.ShouldBeNull();
        config.CreatedAt.ShouldBe(TestClock.UtcNow);
    }

    /// <summary>With valid data: <c>Create</c> slots have correct names and times.</summary>
    [Fact]
    public void Create_WithValidData_SlotsHaveCorrectNamesAndTimes()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);

        var slots = config.Slots.OrderBy(s => s.SortOrder).ToList();
        slots[0].Name.ShouldBe("Breakfast");
        slots[0].DefaultTime.ShouldBe(new TimeOnly(7, 0));
        slots[1].Name.ShouldBe("Lunch");
        slots[2].Name.ShouldBe("Dinner");
    }

    /// <summary>With eight slots: <c>Create</c> succeeds.</summary>
    [Fact]
    public void Create_WithEightSlots_Succeeds()
    {
        var slots = Enumerable.Range(1, 8)
            .Select(i => ($"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), slots, TestClock.UtcNow);

        config.Slots.Count.ShouldBe(8);
    }

    /// <summary>With null user id: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithMissingPersonId_ThrowsDomainException()
    {
        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Empty, DefaultSlots, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>With zero slots: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithZeroSlots_ThrowsDomainException()
    {
        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), [], TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("at least 1 slot");
    }

    /// <summary>With nine slots: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithNineSlots_ThrowsDomainException()
    {
        var slots = Enumerable.Range(1, 9)
            .Select(i => ($"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), slots, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("more than 8 slots");
    }

    /// <summary>Keeping same slots: <c>ApplyUpdate</c> preserves ids and updates fields.</summary>
    [Fact]
    public void ApplyUpdate_KeepingSameSlots_PreservesIdsAndUpdatesFields()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Brunch", new TimeOnly(10, 0)),
            (existing[1].Id, "Lunch", new TimeOnly(12, 0)),
            (existing[2].Id, "Dinner", new TimeOnly(19, 0))), TestClock.UtcNow);

        var updated = config.Slots.OrderBy(s => s.SortOrder).ToList();
        updated[0].Id.ShouldBe(existing[0].Id);
        updated[0].Name.ShouldBe("Brunch");
        updated[0].DefaultTime.ShouldBe(new TimeOnly(10, 0));
        updated[2].DefaultTime.ShouldBe(new TimeOnly(19, 0));
    }

    /// <summary>Adding new slot: <c>ApplyUpdate</c> assigns fresh id.</summary>
    [Fact]
    public void ApplyUpdate_AddingNewSlot_AssignsFreshId()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[1].Id, "Lunch", existing[1].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime),
            (null, "Snack", new TimeOnly(15, 0))), TestClock.UtcNow);

        config.Slots.Count.ShouldBe(4);
        var newSlot = config.Slots.Single(s => s.Name == "Snack");
        newSlot.Id.ShouldNotBeNull();
        newSlot.SortOrder.ShouldBe(3);
    }

    /// <summary>Omitting slot: <c>ApplyUpdate</c> removes it.</summary>
    [Fact]
    public void ApplyUpdate_OmittingSlot_RemovesIt()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        config.ApplyUpdate(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime)), TestClock.UtcNow);

        config.Slots.Count.ShouldBe(2);
        config.Slots.Any(s => s.Id == existing[1].Id).ShouldBeFalse();
    }

    /// <summary><c>ComputeRemovedSlots</c> returns only absent ids.</summary>
    [Fact]
    public void ComputeRemovedSlots_ReturnsOnlyAbsentIds()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);
        var existing = config.Slots.OrderBy(s => s.SortOrder).ToList();

        var removed = config.ComputeRemovedSlots(Upserts(
            (existing[0].Id, "Breakfast", existing[0].DefaultTime),
            (existing[2].Id, "Dinner", existing[2].DefaultTime)));

        removed.Count.ShouldBe(1);
        removed[0].ShouldBe(existing[1].Id);
    }

    /// <summary>With unknown id: <c>ApplyUpdate</c> throws domain exception.</summary>
    [Fact]
    public void ApplyUpdate_WithUnknownId_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);

        var act = () => config.ApplyUpdate(Upserts((MealSlotId.New(), "Bogus", new TimeOnly(8, 0))), TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("Unknown meal slot");
    }

    /// <summary><c>ApplyUpdate</c> sets updated at.</summary>
    [Fact]
    public void ApplyUpdate_SetsUpdatedAt()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);

        config.ApplyUpdate(Upserts((null, "Single", new TimeOnly(8, 0))), TestClock.UtcNow);

        config.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With zero slots: <c>ApplyUpdate</c> throws domain exception.</summary>
    [Fact]
    public void ApplyUpdate_WithZeroSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);

        var act = () => config.ApplyUpdate([], TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("at least 1 slot");
    }

    /// <summary>With nine slots: <c>ApplyUpdate</c> throws domain exception.</summary>
    [Fact]
    public void ApplyUpdate_WithNineSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), DefaultSlots, TestClock.UtcNow);
        var tooMany = Enumerable.Range(1, 9)
            .Select(i => new MealSlotUpsert(null, $"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        var act = () => config.ApplyUpdate(tooMany, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("more than 8 slots");
    }
}
