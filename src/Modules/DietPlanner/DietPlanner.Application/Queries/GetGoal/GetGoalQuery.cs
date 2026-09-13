namespace DietPlanner.Application.Queries.GetGoal;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's nutrition goal; <see langword="null"/> when none has been created.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetGoalQuery(string UserId) : IQuery<GoalDto?>;
