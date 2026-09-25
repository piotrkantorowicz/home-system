namespace DietPlanner.Domain.Events;

using Shared.Abstractions.Core.Domain;

/// <summary>
/// Raised whenever a weight entry is created or its weight changed. Handled in-module to refresh the
/// profile's current weight and to check the goal's weight milestone; the milestone, if reached,
/// is republished as <c>GoalMilestoneReachedIntegrationEvent</c>.
/// </summary>
/// <param name="PersonId">Person identifier of the owner.</param>
/// <param name="WeightKg">The recorded weight in kilograms.</param>
/// <param name="Date">The day of the weigh-in.</param>
public sealed record WeightEntryAddedDomainEvent(
    Guid PersonId,
    decimal WeightKg,
    DateOnly Date) : IDomainEvent;
