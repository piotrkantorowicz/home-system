namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>HydrationConfig</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class HydrationConfigTests
{
    /// <summary>With valid data: <c>Create</c> creates config with defaults.</summary>
    [Fact]
    public void Create_WithValidData_CreatesConfigWithDefaults()
    {
        var id = HydrationConfigId.New();

        var config = HydrationConfig.Create(id, Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.UtcNow);

        config.Id.ShouldBe(id);
        config.PersonId.ShouldBe(Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"));
        config.DailyWaterTargetMl.ShouldBe(2500);
        config.GlassSizeMl.ShouldBe(250);
        config.TrackWaterIntake.ShouldBeTrue();
    }

    /// <summary>With custom values: <c>Create</c> creates config.</summary>
    [Fact]
    public void Create_WithCustomValues_CreatesConfig()
    {
        var config = HydrationConfig.Create(HydrationConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.UtcNow, 3000, 300, false);

        config.DailyWaterTargetMl.ShouldBe(3000);
        config.GlassSizeMl.ShouldBe(300);
        config.TrackWaterIntake.ShouldBeFalse();
    }

    /// <summary>With null user id: <c>Create</c> throws argument exception.</summary>
    [Fact]
    public void Create_WithMissingPersonId_ThrowsArgumentException()
    {
        var act = () => HydrationConfig.Create(HydrationConfigId.New(), Guid.Empty, TestClock.UtcNow);

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary>With new values: <c>Update</c> updates config.</summary>
    [Fact]
    public void Update_WithNewValues_UpdatesConfig()
    {
        var config = HydrationConfig.Create(HydrationConfigId.New(), Guid.Parse("d35a2a2a-d1d1-55ed-90a7-348c3da59deb"), TestClock.UtcNow);

        config.Update(3000, 300, false, TestClock.UtcNow);

        config.DailyWaterTargetMl.ShouldBe(3000);
        config.GlassSizeMl.ShouldBe(300);
        config.TrackWaterIntake.ShouldBeFalse();
    }
}
