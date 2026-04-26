namespace DietPlanner.Contracts.Events;

using Shared.Abstractions.Messaging;

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
