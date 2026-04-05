namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;

public sealed class WaterIntakeTests
{
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

    [Fact]
    public void Create_WithNullNote_CreatesEntry()
    {
        var intake = WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), 500, null);

        intake.Note.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), null!, DateOnly.FromDateTime(DateTime.UtcNow), 250, null);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Create_WithZeroAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), 0, null);

        act.ShouldThrow<DietPlannerDomainException>()
           .Message.ShouldContain("greater than zero");
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainException()
    {
        var act = () => WaterIntake.Create(WaterIntakeId.New(), "user-1", DateOnly.FromDateTime(DateTime.UtcNow), -100, null);

        act.ShouldThrow<DietPlannerDomainException>();
    }
}
