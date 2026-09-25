namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Events;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>WeightEntry</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class WeightEntryTests
{
    private static readonly DateOnly Today = TestClock.Today;

    /// <summary>With valid input: <c>Create</c> creates entry.</summary>
    [Fact]
    public void Create_WithValidInput_CreatesEntry()
    {
        var id = WeightEntryId.New();
        var date = Today.AddDays(-1);

        WeightEntry entry = WeightEntry.Create(id, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), date, 80.5m, TestClock.UtcNow);

        entry.Id.ShouldBe(id);
        entry.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        entry.Date.ShouldBe(date);
        entry.WeightKg.ShouldBe(80.5m);
        entry.CreatedAt.ShouldNotBe(default);
        entry.UpdatedAt.ShouldBeNull();
    }

    /// <summary>With empty user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithEmptyPersonId_ThrowsArgumentException()
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), Guid.Empty, Today, 80m, TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With non positive weight: <c>Create</c> throws domain exception.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void Create_WithNonPositiveWeight_ThrowsDomainException(decimal weight)
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, weight, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>With excessive weight: <c>Create</c> throws domain exception.</summary>
    [Theory]
    [InlineData(1000)]
    [InlineData(1500)]
    public void Create_WithExcessiveWeight_ThrowsDomainException(decimal weight)
    {
        var act = () => WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, weight, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>With future date: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithFutureDate_ThrowsDomainException()
    {
        var future = Today.AddDays(1);

        var act = () => WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), future, 80m, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }

    /// <summary>With valid weight: <c>ChangeWeight</c> updates value and stamps updated at.</summary>
    [Fact]
    public void ChangeWeight_WithValidWeight_UpdatesValueAndStampsUpdatedAt()
    {
        WeightEntry entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m, TestClock.UtcNow);

        entry.ChangeWeight(82.3m, TestClock.UtcNow);

        entry.WeightKg.ShouldBe(82.3m);
        entry.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With invalid weight: <c>ChangeWeight</c> throws domain exception.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1500)]
    public void ChangeWeight_WithInvalidWeight_ThrowsDomainException(decimal weight)
    {
        WeightEntry entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 80m, TestClock.UtcNow);

        var act = () => entry.ChangeWeight(weight, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}

/// <summary>Unit tests for <c>WeightEntryDomainEvent</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class WeightEntryDomainEventTests
{
    private static readonly DateOnly Today = TestClock.Today;

    /// <summary><c>Create</c> raises weight entry added domain event.</summary>
    [Fact]
    public void Create_RaisesWeightEntryAddedDomainEvent()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 75m, TestClock.UtcNow);

        var domainEvent = entry.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<WeightEntryAddedDomainEvent>();
        domainEvent.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        domainEvent.WeightKg.ShouldBe(75m);
        domainEvent.Date.ShouldBe(Today);
    }

    /// <summary><c>ChangeWeight</c> raises weight entry added domain event.</summary>
    [Fact]
    public void ChangeWeight_RaisesWeightEntryAddedDomainEvent()
    {
        var entry = WeightEntry.Create(WeightEntryId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), Today, 75m, TestClock.UtcNow);
        entry.ClearDomainEvents();

        entry.ChangeWeight(74m, TestClock.UtcNow);

        var domainEvent = entry.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<WeightEntryAddedDomainEvent>();
        domainEvent.WeightKg.ShouldBe(74m);
    }
}
