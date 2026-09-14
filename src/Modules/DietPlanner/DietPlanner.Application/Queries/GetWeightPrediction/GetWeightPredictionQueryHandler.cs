namespace DietPlanner.Application.Queries.GetWeightPrediction;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Cqrs;

internal sealed class GetWeightPredictionQueryHandler
    : IQueryHandler<GetWeightPredictionQuery, WeightPredictionDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;

    public GetWeightPredictionQueryHandler(IDietPlannerReadDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<WeightPredictionDto?> HandleAsync(
        GetWeightPredictionQuery query,
        CancellationToken ct = default)
    {
        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == query.UserId)
            .Select(p => new
            {
                p.DateOfBirth,
                p.Gender,
                p.HeightCm,
                p.CurrentWeightKg,
                p.TargetWeightKg,
                p.ActivityLevel
            })
            .FirstOrDefaultAsync(ct);

        if (profile is null)
            return null;

        // Need all required fields to compute a prediction
        if (profile.DateOfBirth is null
            || profile.Gender is null
            || profile.HeightCm is null
            || profile.CurrentWeightKg is null
            || profile.ActivityLevel is null)
            return null;

        int ageYears = CalculateAge(profile.DateOfBirth.Value);
        Gender gender = profile.Gender.Value;
        decimal heightCm = profile.HeightCm.Value;
        decimal currentWeightKg = profile.CurrentWeightKg.Value;

        decimal bmr = WeightPredictionService.CalculateBmr(currentWeightKg, heightCm, ageYears, gender);
        decimal tdee = WeightPredictionService.CalculateTdee(bmr, profile.ActivityLevel.Value);
        decimal dailyDeficit = tdee - query.DailyCalorieTarget;
        decimal weeklyWeightChange = WeightPredictionService.CalculateWeeklyWeightChange(tdee, query.DailyCalorieTarget);
        decimal currentBmi = WeightPredictionService.CalculateBmi(currentWeightKg, heightCm);

        DateOnly? estimatedGoalDate = null;
        decimal? targetBmi = null;

        if (profile.TargetWeightKg.HasValue)
        {
            estimatedGoalDate = WeightPredictionService.EstimateGoalDate(
                currentWeightKg,
                profile.TargetWeightKg.Value,
                weeklyWeightChange);

            targetBmi = WeightPredictionService.CalculateBmi(profile.TargetWeightKg.Value, heightCm);
        }

        return new WeightPredictionDto(
            Bmr: Math.Round(bmr, 1),
            Tdee: tdee,
            DailyDeficit: Math.Round(dailyDeficit, 1),
            WeeklyWeightChange: weeklyWeightChange,
            EstimatedGoalDate: estimatedGoalDate,
            CurrentBmi: currentBmi,
            TargetBmi: targetBmi);
    }

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int age = today.Year - dateOfBirth.Year;

        if (today < dateOfBirth.AddYears(age))
            age--;

        return age;
    }
}
