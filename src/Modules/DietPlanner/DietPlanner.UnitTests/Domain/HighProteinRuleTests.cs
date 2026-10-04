namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Services;

/// <summary>The approved high-protein definition: at least 25 g per serving, or at least 30% of calories from protein.</summary>
public sealed class HighProteinRuleTests
{
    /// <summary>Exactly 25 g qualifies, just below does not (calories too high for the share test).</summary>
    [Theory]
    [InlineData(25, 800, true)]
    [InlineData(24.99, 800, false)]
    public void Grams_AreInclusiveAt25(decimal protein, decimal calories, bool expected)
        => HighProteinRule.IsHighProtein(protein, calories).ShouldBe(expected);

    /// <summary>Exactly 30% of calories from protein qualifies; just below does not.</summary>
    [Theory]
    [InlineData(15, 200, true)]
    [InlineData(14.9, 200, false)]
    public void CalorieShare_IsInclusiveAt30Percent(decimal protein, decimal calories, bool expected)
        => HighProteinRule.IsHighProtein(protein, calories).ShouldBe(expected);

    /// <summary>Unknown or zero calories leave only the grams test.</summary>
    [Fact]
    public void MissingCalories_FallBackToGrams()
    {
        HighProteinRule.IsHighProtein(10, null).ShouldBeFalse();
        HighProteinRule.IsHighProtein(10, 0).ShouldBeFalse();
        HighProteinRule.IsHighProtein(25, null).ShouldBeTrue();
    }
}
