namespace DietPlanner.Application.Workers;

internal sealed record WeeklySummaryCandidate(
    Guid PersonId,
    string Locale,
    DayOfWeek WeeklySummaryDayOfWeekUtc,
    TimeOnly WeeklySummaryTimeOfDayUtc,
    DateTime? LastWeeklySummaryAt);
