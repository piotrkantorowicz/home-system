namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;

/// <summary>Unit tests for <c>WeightPredictionService</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class WeightPredictionServiceTests
{
    // ── CalculateWeeklyWeightChange ──────────────────────────────────────────

    /// <summary>When calories below tdee: calculate weekly weight change returns negative.</summary>
    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesBelowTdee_ReturnsNegative()
    {
        // 500 kcal deficit → ~0.45 kg/week loss
        decimal result = WeightPredictionService.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 1500m);

        result.ShouldBeLessThan(0);
    }

    /// <summary>When calories above tdee: calculate weekly weight change returns positive.</summary>
    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesAboveTdee_ReturnsPositive()
    {
        // 500 kcal surplus → ~0.45 kg/week gain
        decimal result = WeightPredictionService.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 2500m);

        result.ShouldBeGreaterThan(0);
    }

    /// <summary>When calories equal tdee: calculate weekly weight change returns zero.</summary>
    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesEqualTdee_ReturnsZero()
    {
        decimal result = WeightPredictionService.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 2000m);

        result.ShouldBe(0m);
    }

    /// <summary>Calculate weekly weight change matches formula.</summary>
    [Theory]
    [InlineData(2000, 1500, -0.455)]  // 500 deficit × 7 / 7700
    [InlineData(2000, 2500, 0.455)]  // 500 surplus × 7 / 7700
    [InlineData(2000, 1000, -0.909)]  // 1000 deficit × 7 / 7700
    public void CalculateWeeklyWeightChange_MatchesFormula(
        decimal tdee, decimal target, decimal expected)
    {
        decimal result = WeightPredictionService.CalculateWeeklyWeightChange(tdee, target);

        result.ShouldBe(expected, tolerance: 0.001m);
    }

    // ── EstimateGoalDate ─────────────────────────────────────────────────────

    /// <summary>When losing towards lower target: <c>EstimateGoalDate</c> returns date.</summary>
    [Fact]
    public void EstimateGoalDate_WhenLosingTowardsLowerTarget_ReturnsDate()
    {
        // current=90, target=80 → losing 0.5 kg/week → ~20 weeks out
        DateOnly? result = WeightPredictionService.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: -0.5m,
            now: TestClock.UtcNow);

        result.ShouldNotBeNull();
        result!.Value.ShouldBeGreaterThan(TestClock.Today);
    }

    /// <summary>When gaining towards higher target: <c>EstimateGoalDate</c> returns date.</summary>
    [Fact]
    public void EstimateGoalDate_WhenGainingTowardsHigherTarget_ReturnsDate()
    {
        DateOnly? result = WeightPredictionService.EstimateGoalDate(
            currentWeightKg: 70m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0.5m,
            now: TestClock.UtcNow);

        result.ShouldNotBeNull();
    }

    /// <summary>When change is in wrong direction: <c>EstimateGoalDate</c> returns null.</summary>
    [Fact]
    public void EstimateGoalDate_WhenChangeIsInWrongDirection_ReturnsNull()
    {
        // Want to lose (target < current) but gaining — can never reach
        DateOnly? result = WeightPredictionService.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0.5m,
            now: TestClock.UtcNow);

        result.ShouldBeNull();
    }

    /// <summary>When weekly change is zero: <c>EstimateGoalDate</c> returns null.</summary>
    [Fact]
    public void EstimateGoalDate_WhenWeeklyChangeIsZero_ReturnsNull()
    {
        DateOnly? result = WeightPredictionService.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0m,
            now: TestClock.UtcNow);

        result.ShouldBeNull();
    }

    /// <summary>When already at target: <c>EstimateGoalDate</c> returns today date.</summary>
    [Fact]
    public void EstimateGoalDate_WhenAlreadyAtTarget_ReturnsTodayDate()
    {
        DateOnly? result = WeightPredictionService.EstimateGoalDate(
            currentWeightKg: 80m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: -0.5m,
            now: TestClock.UtcNow);

        result.ShouldBe(TestClock.Today);
    }

    // ── CalculateBmr ────────────────────────────────────────────────────────

    /// <summary><c>CalculateBmr</c> returns expected value.</summary>
    [Theory]
    [InlineData(80, 180, 30, Gender.Male, 1780.0)]  // 10*80 + 6.25*180 - 5*30 + 5
    [InlineData(60, 165, 25, Gender.Female, 1345.25)] // 10*60 + 6.25*165 - 5*25 - 161
    public void CalculateBmr_ReturnsExpectedValue(
        decimal weight, decimal height, int age, Gender gender, double expected)
    {
        decimal result = WeightPredictionService.CalculateBmr(weight, height, age, gender);

        ((double)result).ShouldBe(expected, tolerance: 0.5);
    }
}
