namespace DietPlanner.Application.Queries.GetGoal;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's nutrition goal; <see langword="null"/> when none has been created.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
public sealed record GetGoalQuery(Guid PersonId) : IQuery<GoalDto?>;
