namespace DietPlanner.Application.Queries.GetWaterIntake;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Lists the caller's drinks on one day together with the day's total and target.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
/// <param name="Date">The calendar day.</param>
public sealed record GetWaterIntakeQuery(Guid PersonId, DateOnly Date) : IQuery<WaterIntakeListDto>;
