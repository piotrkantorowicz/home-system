namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

public sealed class HydrationConfigTests
{
    [Fact]
    public void Create_WithValidData_CreatesConfigWithDefaults()
    {
        var id = HydrationConfigId.New();

        var config = HydrationConfig.Create(id, "user-1");

        config.Id.ShouldBe(id);
        config.UserId.ShouldBe("user-1");
        config.DailyWaterTargetMl.ShouldBe(2500);
        config.GlassSizeMl.ShouldBe(250);
        config.TrackWaterIntake.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithCustomValues_CreatesConfig()
    {
        var config = HydrationConfig.Create(HydrationConfigId.New(), "user-1", 3000, 300, false);

        config.DailyWaterTargetMl.ShouldBe(3000);
        config.GlassSizeMl.ShouldBe(300);
        config.TrackWaterIntake.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentException()
    {
        var act = () => HydrationConfig.Create(HydrationConfigId.New(), null!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Update_WithNewValues_UpdatesConfig()
    {
        var config = HydrationConfig.Create(HydrationConfigId.New(), "user-1");

        config.Update(3000, 300, false);

        config.DailyWaterTargetMl.ShouldBe(3000);
        config.GlassSizeMl.ShouldBe(300);
        config.TrackWaterIntake.ShouldBeFalse();
    }
}
