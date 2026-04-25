namespace DietPlanner.Application.Queries.GetGoal;

using Shared.Abstractions.Cqrs;

public sealed record GetGoalQuery(string UserId) : IQuery<GoalDto?>;
