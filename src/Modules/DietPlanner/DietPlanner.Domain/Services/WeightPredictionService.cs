namespace DietPlanner.Domain.Services;

using DietPlanner.Domain.ValueObjects;

/// <summary>
/// Pure arithmetic behind the weight prediction screen: Mifflin-St Jeor BMR, activity-adjusted
/// TDEE, BMI, the weekly change implied by a calorie target and the resulting goal date. Static — its
/// inputs come from <see cref="Aggregates.UserProfile"/> and <see cref="Aggregates.UserGoal"/>.
/// </summary>
public static class WeightPredictionService
{
    private static readonly Dictionary<ActivityLevel, decimal> ActivityMultipliers = new()
    {
        { ActivityLevel.Sedentary, 1.2m },
        { ActivityLevel.LightlyActive, 1.375m },
        { ActivityLevel.ModeratelyActive, 1.55m },
        { ActivityLevel.VeryActive, 1.725m },
        { ActivityLevel.ExtraActive, 1.9m }
    };

    /// <summary>
    /// Calculates BMR using the Mifflin-St Jeor formula.
    /// Male:   10 * weight + 6.25 * height - 5 * age + 5
    /// Female: 10 * weight + 6.25 * height - 5 * age - 161
    /// Other:  average of Male and Female
    /// </summary>
    public static decimal CalculateBmr(decimal weightKg, decimal heightCm, int ageYears, Gender gender)
    {
        decimal base_ = 10m * weightKg + 6.25m * heightCm - 5m * ageYears;

        return gender switch
        {
            Gender.Male => base_ + 5m,
            Gender.Female => base_ - 161m,
            _ => base_ - 78m   // average of +5 and -161
        };
    }

    /// <summary>
    /// TDEE = BMR * activity multiplier.
    /// </summary>
    public static decimal CalculateTdee(decimal bmr, ActivityLevel activityLevel)
        => Math.Round(bmr * ActivityMultipliers[activityLevel], 1);

    /// <summary>
    /// Weekly weight change in kg.
    /// Positive = gain (surplus), Negative = loss (deficit).
    /// Formula: (calorieTarget - TDEE) * 7 / 7700
    /// </summary>
    public static decimal CalculateWeeklyWeightChange(decimal tdee, decimal dailyCalorieTarget)
        => Math.Round((dailyCalorieTarget - tdee) * 7m / 7700m, 3);

    /// <summary>
    /// BMI = weight (kg) / height (m)^2
    /// </summary>
    public static decimal CalculateBmi(decimal weightKg, decimal heightCm)
    {
        var heightM = heightCm / 100m;
        return Math.Round(weightKg / (heightM * heightM), 1);
    }

    /// <summary>
    /// Estimates the date when the target weight will be reached. Returns today (UTC) when the
    /// current and target weight already differ by less than 0.01 kg, and null when the weekly
    /// change is zero or moves away from the target.
    /// </summary>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static DateOnly? EstimateGoalDate(
        decimal currentWeightKg,
        decimal targetWeightKg,
        decimal weeklyWeightChangeKg,
        DateTime now)
    {
        decimal weightDelta = targetWeightKg - currentWeightKg;

        // Already at target
        if (Math.Abs(weightDelta) < 0.01m)
            return DateOnly.FromDateTime(now);

        // Can't reach target — change is in wrong direction or zero
        if (weeklyWeightChangeKg == 0m)
            return null;

        bool wantToLose = weightDelta < 0;
        bool isLosing = weeklyWeightChangeKg < 0;

        if (wantToLose != isLosing)
            return null;

        decimal weeksNeeded = Math.Abs(weightDelta / weeklyWeightChangeKg);
        double daysNeeded = (double)(weeksNeeded * 7m);

        var goalDate = now.AddDays(daysNeeded);
        return DateOnly.FromDateTime(goalDate);
    }
}
