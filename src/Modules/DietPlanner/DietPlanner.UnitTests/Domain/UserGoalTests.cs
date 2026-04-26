namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public sealed class UserGoalTests
{
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

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => UserGoal.Create(UserGoalId.New(), null!, null, null, null, null, null);

        act.ShouldThrow<ArgumentException>();
    }
}

public sealed class UserGoalMilestoneTests
{
    [Fact]
    public void ShouldEmitWeightMilestone_WhenNoTarget_ReturnsFalse()
    {
        var goal = UserGoal.Create(UserGoalId.New(), "user-1", 2000, null, null, null, null);

        goal.ShouldEmitWeightMilestone(75m).ShouldBeFalse();
    }

    [Fact]
    public void ShouldEmitWeightMilestone_WhenAboveTarget_ReturnsFalse()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.ShouldEmitWeightMilestone(75m).ShouldBeFalse();
    }

    [Fact]
    public void ShouldEmitWeightMilestone_WhenAtOrBelowTarget_ReturnsTrue()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.ShouldEmitWeightMilestone(70m).ShouldBeTrue();
        goal.ShouldEmitWeightMilestone(69.5m).ShouldBeTrue();
    }

    [Fact]
    public void ShouldEmitWeightMilestone_AfterMarkAchieved_ReturnsFalse()
    {
        var goal = UserGoal.Create(
            UserGoalId.New(), "user-1", 2000, null, null, null, null,
            targetWeightKg: 70m);

        goal.MarkMilestoneAchieved(DateTime.UtcNow);

        goal.ShouldEmitWeightMilestone(65m).ShouldBeFalse();
    }

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
