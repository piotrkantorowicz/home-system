namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>WaterIntake</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class WaterIntakeTests
{
    /// <summary>With valid data: <c>Create</c> creates entry.</summary>
    [Fact]
    public void Create_WithValidData_CreatesEntry()
    {
        var id = WaterIntakeId.New();
        var date = TestClock.Today;

        var intake = WaterIntake.Create(id, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), date, 250, "Morning glass", TestClock.UtcNow);

        intake.Id.ShouldBe(id);
        intake.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        intake.Date.ShouldBe(date);
        intake.AmountMl.ShouldBe(250);
        intake.Note.ShouldBe("Morning glass");
    }

    /// <summary>With null note: <c>Create</c> creates entry.</summary>
    [Fact]
    public void Create_WithNullNote_CreatesEntry()
    {
        var intake = WaterIntake.Create(WaterIntakeId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.Today, 500, null, TestClock.UtcNow);

        intake.Note.ShouldBeNull();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithMissingPersonId_ThrowsArgumentException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), Guid.Empty, TestClock.Today, 250, null, TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With zero amount: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithZeroAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.Today, 0, null, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>()
           .Message.ShouldContain("greater than zero");
    }

    /// <summary>With negative amount: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.Today, -100, null, TestClock.UtcNow);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
