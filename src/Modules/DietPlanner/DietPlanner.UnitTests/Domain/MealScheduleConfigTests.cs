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
    public void Create_WithOneSlot_Succeeds()
    {
        IReadOnlyList<(string Name, TimeOnly DefaultTime)> slots = [("Breakfast", new TimeOnly(8, 0))];

        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", slots);

        config.Slots.Count.ShouldBe(1);
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
    public void Create_WithWhitespaceUserId_ThrowsDomainException()
    {
        var act = () => MealScheduleConfig.Create(MealScheduleConfigId.New(), "   ", DefaultSlots);

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
    public void UpdateSlots_WithValidSlots_ReplacesAllSlots()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        IReadOnlyList<(string Name, TimeOnly DefaultTime)> newSlots =
        [
            ("Morning Meal", new TimeOnly(6, 30)),
            ("Evening Meal", new TimeOnly(19, 0))
        ];

        config.UpdateSlots(newSlots);

        config.Slots.Count.ShouldBe(2);
        config.Slots.OrderBy(s => s.SortOrder).First().Name.ShouldBe("Morning Meal");
    }

    [Fact]
    public void UpdateSlots_SetsUpdatedAt()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        config.UpdateSlots(DefaultSlots);

        config.UpdatedAt.ShouldNotBeNull();
        config.UpdatedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void UpdateSlots_WithZeroSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);

        var act = () => config.UpdateSlots([]);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("at least 1 slot");
    }

    [Fact]
    public void UpdateSlots_WithNineSlots_ThrowsDomainException()
    {
        MealScheduleConfig config = MealScheduleConfig.Create(MealScheduleConfigId.New(), "user-1", DefaultSlots);
        var tooMany = Enumerable.Range(1, 9)
            .Select(i => ($"Slot {i}", new TimeOnly(6 + i, 0)))
            .ToList();

        var act = () => config.UpdateSlots(tooMany);

        act.ShouldThrow<DietPlannerDomainException>().Message.ShouldContain("more than 8 slots");
    }
}
