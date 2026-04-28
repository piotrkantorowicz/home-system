namespace DietPlanner.Application.Workers;

public sealed record MealReminderCandidate(
    string UserId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAtUtc);
