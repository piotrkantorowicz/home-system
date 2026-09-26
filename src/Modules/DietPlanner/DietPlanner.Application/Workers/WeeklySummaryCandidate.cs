namespace DietPlanner.Application.Workers;

internal sealed record WeeklySummaryCandidate(
    Guid PersonId,
    string Locale,
    TimeZoneInfo TimeZone,
    DayOfWeek WeeklySummaryDayOfWeek,
    TimeOnly WeeklySummaryTimeOfDay,
    DateTime? LastWeeklySummaryAt);
