namespace DietPlanner.Application.Queries.GetWeightEntries;

public sealed record WeightEntryDto(
    Guid Id,
    DateOnly Date,
    decimal WeightKg,
    DateTime CreatedAt);
