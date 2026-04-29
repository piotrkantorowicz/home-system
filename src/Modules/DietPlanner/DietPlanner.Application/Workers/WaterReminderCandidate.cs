namespace DietPlanner.Application.Workers;

internal sealed record WaterReminderCandidate(
    string UserId,
    string Locale,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStartUtc,
    TimeOnly WaterWindowEndUtc,
    DateTime? LastWaterReminderAt);
