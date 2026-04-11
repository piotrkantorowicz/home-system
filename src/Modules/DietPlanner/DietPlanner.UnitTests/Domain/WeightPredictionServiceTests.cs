namespace DietPlanner.UnitTests.Domain;

using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;

public sealed class WeightPredictionServiceTests
{
    private readonly WeightPredictionService _sut = new();

    // ── CalculateWeeklyWeightChange ──────────────────────────────────────────

    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesBelowTdee_ReturnsNegative()
    {
        // 500 kcal deficit → ~0.45 kg/week loss
        decimal result = _sut.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 1500m);

        result.ShouldBeLessThan(0);
    }

    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesAboveTdee_ReturnsPositive()
    {
        // 500 kcal surplus → ~0.45 kg/week gain
        decimal result = _sut.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 2500m);

        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CalculateWeeklyWeightChange_WhenCaloriesEqualTdee_ReturnsZero()
    {
        decimal result = _sut.CalculateWeeklyWeightChange(tdee: 2000m, dailyCalorieTarget: 2000m);

        result.ShouldBe(0m);
    }

    [Theory]
    [InlineData(2000, 1500, -0.455)]  // 500 deficit × 7 / 7700
    [InlineData(2000, 2500,  0.455)]  // 500 surplus × 7 / 7700
    [InlineData(2000, 1000, -0.909)]  // 1000 deficit × 7 / 7700
    public void CalculateWeeklyWeightChange_MatchesFormula(
        decimal tdee, decimal target, decimal expected)
    {
        decimal result = _sut.CalculateWeeklyWeightChange(tdee, target);

        result.ShouldBe(expected, tolerance: 0.001m);
    }

    // ── EstimateGoalDate ─────────────────────────────────────────────────────

    [Fact]
    public void EstimateGoalDate_WhenLosingTowardsLowerTarget_ReturnsDate()
    {
        // current=90, target=80 → losing 0.5 kg/week → ~20 weeks out
        DateOnly? result = _sut.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: -0.5m);

        result.ShouldNotBeNull();
        result!.Value.ShouldBeGreaterThan(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void EstimateGoalDate_WhenGainingTowardsHigherTarget_ReturnsDate()
    {
        DateOnly? result = _sut.EstimateGoalDate(
            currentWeightKg: 70m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0.5m);

        result.ShouldNotBeNull();
    }

    [Fact]
    public void EstimateGoalDate_WhenChangeIsInWrongDirection_ReturnsNull()
    {
        // Want to lose (target < current) but gaining — can never reach
        DateOnly? result = _sut.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0.5m);

        result.ShouldBeNull();
    }

    [Fact]
    public void EstimateGoalDate_WhenWeeklyChangeIsZero_ReturnsNull()
    {
        DateOnly? result = _sut.EstimateGoalDate(
            currentWeightKg: 90m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: 0m);

        result.ShouldBeNull();
    }

    [Fact]
    public void EstimateGoalDate_WhenAlreadyAtTarget_ReturnsTodayDate()
    {
        DateOnly? result = _sut.EstimateGoalDate(
            currentWeightKg: 80m,
            targetWeightKg: 80m,
            weeklyWeightChangeKg: -0.5m);

        result.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    // ── CalculateBmr ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(80, 180, 30, Gender.Male,   1780.0)]  // 10*80 + 6.25*180 - 5*30 + 5
    [InlineData(60, 165, 25, Gender.Female, 1345.25)] // 10*60 + 6.25*165 - 5*25 - 161
    public void CalculateBmr_ReturnsExpectedValue(
        decimal weight, decimal height, int age, Gender gender, double expected)
    {
        decimal result = _sut.CalculateBmr(weight, height, age, gender);

        ((double)result).ShouldBe(expected, tolerance: 0.5);
    }
}
