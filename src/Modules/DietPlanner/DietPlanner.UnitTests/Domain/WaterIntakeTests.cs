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
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var intake = WaterIntake.Create(id, "user-1", date, 250, "Morning glass");

        intake.Id.ShouldBe(id);
        intake.UserId.ShouldBe("user-1");
        intake.Date.ShouldBe(date);
        intake.AmountMl.ShouldBe(250);
        intake.Note.ShouldBe("Morning glass");
    }

    /// <summary>With null note: <c>Create</c> creates entry.</summary>
    [Fact]
    public void Create_WithNullNote_CreatesEntry()
    {
        var intake = WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), 500, null);

        intake.Note.ShouldBeNull();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), null!, DateOnly.FromDateTime(DateTime.UtcNow), 250, null);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With zero amount: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithZeroAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), 0, null);

        act.ShouldThrow<DietPlannerDomainException>()
           .Message.ShouldContain("greater than zero");
    }

    /// <summary>With negative amount: <c>Create</c> throws domain exception.</summary>
    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), -100, null);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
