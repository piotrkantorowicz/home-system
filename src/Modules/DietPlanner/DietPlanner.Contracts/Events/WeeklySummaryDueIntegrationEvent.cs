namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

/// <summary>
/// Published by the weekly summary job at the user's configured day and time with the previous
/// week's totals already computed, so the consumer only renders them. Consumed by Notifications.
/// </summary>
/// <param name="EventId">Unique identity of this occurrence; consumers de-duplicate on it.</param>
/// <param name="OccurredAt">When the event was raised, UTC.</param>
/// <param name="UserId">Auth subject of the user the notification is for.</param>
/// <param name="Locale">The user's locale (e.g. <c>en</c>, <c>pl</c>) for rendering the notification text.</param>
/// <param name="WeekStart">First day of the summarised week.</param>
/// <param name="WeekEnd">Last day of the summarised week.</param>
/// <param name="TotalKcal">Energy consumed over the week, kcal.</param>
/// <param name="TargetKcal">The user's daily calorie target multiplied by seven, kcal; 0 when no target is set.</param>
/// <param name="AvgWaterLiters">Average daily water intake, litres.</param>
/// <param name="WeightDeltaKg">Weight change from the first to the last weigh-in of the week, kilograms; <see langword="null"/> without two weigh-ins.</param>
/// <param name="MealsCompleted">Planned meals marked done or modified.</param>
/// <param name="MealsPlanned">Planned meals in the week.</param>
public sealed record WeeklySummaryDueIntegrationEvent(
    Guid EventId,
    DateTime OccurredAt,
    string UserId,
    string Locale,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    int TotalKcal,
    int TargetKcal,
    decimal AvgWaterLiters,
    decimal? WeightDeltaKg,
    int MealsCompleted,
    int MealsPlanned) : IIntegrationEvent;
