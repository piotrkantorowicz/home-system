namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>UserGoal</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class UserGoalTests
{
    /// <summary>With valid data: <c>Create</c> creates goal.</summary>
    [Fact]
    public void Create_WithValidData_CreatesGoal()
    {
        var id = UserGoalId.New();

        var goal = UserGoal.Create(id, "user-1", 2000, 150m, 250m, 70m, 30m);

        goal.Id.ShouldBe(id);
        goal.UserId.ShouldBe("user-1");
        goal.DailyCalorieTarget.ShouldBe(2000);
        goal.ProteinGrams.ShouldBe(150m);
        goal.CarbsGrams.ShouldBe(250m);
        goal.FatGrams.ShouldBe(70m);
        goal.FiberGrams.ShouldBe(30m);
        goal.UpdatedAt.ShouldBeNull();
    }

    /// <summary>With new values: <c>Update</c> updates goal.</summary>
    [Fact]
    public void Update_WithNewValues_UpdatesGoal()
    {
        var goal = UserGoal.Create(UserGoalId.New(), "user-1", 2000, 150m, 250m, 70m, 30m);

        goal.Update(1800, 140m, 200m, 60m, 25m);

        goal.DailyCalorieTarget.ShouldBe(1800);
        goal.ProteinGrams.ShouldBe(140m);
        goal.CarbsGrams.ShouldBe(200m);
        goal.FatGrams.ShouldBe(60m);
        goal.FiberGrams.ShouldBe(25m);
        goal.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => UserGoal.Create(UserGoalId.New(), null!, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }
}

/// <summary>Unit tests for <c>UserGoalMilestone</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class UserGoalMilestoneTests
{
    /// <summary>When no target: should emit weight milestone returns false.</summary>
    [Fact]
    public void ShouldEmitWeightMilestone_WhenNoTarget_ReturnsFalse()
    {
        var goal = UserGoal.Create(UserGoalId.New(), "user-1", 2000, null, null, null, null);

        goal.ShouldEmitWeightMilestone(75m).ShouldBeFalse();
    }

    /// <summary>When above target: should emit weight milestone returns false.</summary>
    [Fact]
    public void ShouldEmitWeightMilestone_WhenAboveTarget_ReturnsFalse()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.ShouldEmitWeightMilestone(75m).ShouldBeFalse();
    }

    /// <summary>When at or below target: should emit weight milestone returns true.</summary>
    [Fact]
    public void ShouldEmitWeightMilestone_WhenAtOrBelowTarget_ReturnsTrue()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.ShouldEmitWeightMilestone(70m).ShouldBeTrue();
        goal.ShouldEmitWeightMilestone(69.5m).ShouldBeTrue();
    }

    /// <summary>After mark achieved: should emit weight milestone returns false.</summary>
    [Fact]
    public void ShouldEmitWeightMilestone_AfterMarkAchieved_ReturnsFalse()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.MarkMilestoneAchieved(DateTime.UtcNow);

        goal.ShouldEmitWeightMilestone(65m).ShouldBeFalse();
    }

    /// <summary>First call: <c>MarkMilestoneAchieved</c> sets timestamp.</summary>
    [Fact]
    public void MarkMilestoneAchieved_FirstCall_SetsTimestamp()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);
        var now = DateTime.UtcNow;

        goal.MarkMilestoneAchieved(now);

        goal.MilestoneAchievedAt.ShouldBe(now);
    }

    /// <summary>Second call: <c>MarkMilestoneAchieved</c> is no op.</summary>
    [Fact]
    public void MarkMilestoneAchieved_SecondCall_IsNoOp()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);
        var first = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var second = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        goal.MarkMilestoneAchieved(first);
        goal.MarkMilestoneAchieved(second);

        goal.MilestoneAchievedAt.ShouldBe(first);
    }
}
