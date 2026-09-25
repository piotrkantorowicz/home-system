namespace DietPlanner.Application.Workers;

internal sealed record MealReminderCandidate(
    Guid PersonId,
    string Locale,
    Guid MealEntryId,
    string MealSlotName,
    DateTime PlannedAtUtc);
