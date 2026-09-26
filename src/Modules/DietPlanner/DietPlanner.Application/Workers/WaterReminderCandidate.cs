namespace DietPlanner.Application.Workers;

internal sealed record WaterReminderCandidate(
    Guid PersonId,
    string Locale,
    TimeZoneInfo TimeZone,
    int WaterReminderIntervalMinutes,
    TimeOnly WaterWindowStart,
    TimeOnly WaterWindowEnd,
    DateTime? LastWaterReminderAt);
