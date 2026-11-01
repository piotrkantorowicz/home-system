namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <see cref="NutritionInfo"/> arithmetic.</summary>
public sealed class NutritionInfoTests
{
    /// <summary><c>Add</c> sums each component into a new instance and leaves both operands unchanged.</summary>
    [Fact]
    public void Add_TwoTotals_SumsEachComponent()
    {
        var first = new NutritionInfo(100, 10, 20, 5, 2);
        var second = new NutritionInfo(50.5m, 1.5m, 0, 2, 0.5m);

        var sum = first.Add(second);

        sum.ShouldBe(new NutritionInfo(150.5m, 11.5m, 20, 7, 2.5m));
        first.ShouldBe(new NutritionInfo(100, 10, 20, 5, 2));
    }
}
