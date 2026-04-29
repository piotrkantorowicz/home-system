namespace DietPlanner.Application.Workers;

internal sealed record WeeklySummaryCandidate(
    string UserId,
    string Locale,
    DayOfWeek WeeklySummaryDayOfWeekUtc,
    TimeOnly WeeklySummaryTimeOfDayUtc,
    DateTime? LastWeeklySummaryAt);
