namespace DietPlanner.Application.Queries.GetWeightPrediction;

using DietPlanner.Application.Persistence;
using DietPlanner.Domain.Services;
using DietPlanner.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.CQRS;

internal sealed class GetWeightPredictionQueryHandler
    : IQueryHandler<GetWeightPredictionQuery, WeightPredictionDto?>
{
    private readonly IDietPlannerReadDbContext _dbContext;
    private readonly WeightPredictionService _predictionService;

    public GetWeightPredictionQueryHandler(
        IDietPlannerReadDbContext dbContext,
        WeightPredictionService predictionService)
    {
        _dbContext = dbContext;
        _predictionService = predictionService;
    }

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

        decimal bmr = _predictionService.CalculateBmr(currentWeightKg, heightCm, ageYears, gender);
        decimal tdee = _predictionService.CalculateTdee(bmr, profile.ActivityLevel.Value);
        decimal dailyDeficit = tdee - query.DailyCalorieTarget;
        decimal weeklyWeightChange = _predictionService.CalculateWeeklyWeightChange(tdee, query.DailyCalorieTarget);
        decimal currentBmi = _predictionService.CalculateBmi(currentWeightKg, heightCm);

        DateOnly? estimatedGoalDate = null;
        decimal? targetBmi = null;

        if (profile.TargetWeightKg.HasValue)
        {
            estimatedGoalDate = _predictionService.EstimateGoalDate(
                currentWeightKg,
                profile.TargetWeightKg.Value,
                weeklyWeightChange);

            targetBmi = _predictionService.CalculateBmi(profile.TargetWeightKg.Value, heightCm);
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
