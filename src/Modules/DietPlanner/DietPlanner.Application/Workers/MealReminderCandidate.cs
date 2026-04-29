namespace DietPlanner.Application.Workers;

internal sealed record MealReminderCandidate(
    string UserId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAtUtc);
