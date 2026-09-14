namespace DietPlanner.Application.Queries.GetWeightEntries;

/// <summary>
/// One weigh-in.
/// </summary>
/// <param name="Id">Identifier of the entry.</param>
/// <param name="Date">The day of the weigh-in.</param>
/// <param name="WeightKg">Weight in kilograms.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
public sealed record WeightEntryDto(
    Guid Id,
    DateOnly Date,
    decimal WeightKg,
    DateTime CreatedAt);
