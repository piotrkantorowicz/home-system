namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Events;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class WeightEntryTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Create_WithValidInput_CreatesEntry()
    {
        var id = WeightEntryId.New();
        var date = Today.AddDays(-1);

        WeightEntry entry = WeightEntry.Create(id, "user-1", date, 80.5m);

        entry.Id.ShouldBe(id);
        entry.UserId.ShouldBe("user-1");
        entry.Date.ShouldBe(date);
        entry.WeightKg.ShouldBe(80.5m);
        entry.CreatedAt.ShouldNotBe(default);
        entry.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), string.Empty, Today, 80m);

        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Create_WithNonPositiveWeight_ThrowsDomainException(decimal weight)
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), "user-1", Today, weight);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(1500)]
    public void Create_WithExcessiveWeight_ThrowsDomainException(decimal weight)
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), "user-1", Today, weight);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void Create_WithFutureDate_ThrowsDomainException()
    {
        var future = Today.AddDays(1);

        var act = () => WeightEntry.Create(WeightEntryId.New(), "user-1", future, 80m);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    [Fact]
    public void ChangeWeight_WithValidWeight_UpdatesValueAndStampsUpdatedAt()
    {
        WeightEntry entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 80m);

        entry.ChangeWeight(82.3m);

        entry.WeightKg.ShouldBe(82.3m);
        entry.UpdatedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1500)]
    public void ChangeWeight_WithInvalidWeight_ThrowsDomainException(decimal weight)
    {
        WeightEntry entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 80m);

        var act = () => entry.ChangeWeight(weight);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}

public sealed class WeightEntryDomainEventTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Create_RaisesWeightEntryAddedDomainEvent()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 75m);

        var domainEvent = entry.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<WeightEntryAddedDomainEvent>();
        domainEvent.UserId.ShouldBe("user-1");
        domainEvent.WeightKg.ShouldBe(75m);
        domainEvent.Date.ShouldBe(Today);
    }

    [Fact]
    public void ChangeWeight_RaisesWeightEntryAddedDomainEvent()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), "user-1", Today, 75m);
        entry.ClearDomainEvents();

        entry.ChangeWeight(74m);

        var domainEvent = entry.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<WeightEntryAddedDomainEvent>();
        domainEvent.WeightKg.ShouldBe(74m);
    }
}
