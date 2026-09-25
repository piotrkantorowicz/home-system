namespace DietPlanner.Application.Workers;

internal sealed record WaterReminderCandidate(
    Guid PersonId,
    string Locale,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStartUtc,
    TimeOnly WaterWindowEndUtc,
    DateTime? LastWaterReminderAt);
