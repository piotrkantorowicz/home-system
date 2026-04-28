namespace DietPlanner.Domain.Events;

using Shared.Abstractions.Core.Domain;

public sealed record WeightEntryAddedDomainEvent(
    string UserId,
    decimal WeightKg,
    DateOnly Date) : IDomainEvent;
