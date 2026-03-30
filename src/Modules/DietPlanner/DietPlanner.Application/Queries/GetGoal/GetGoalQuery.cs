namespace DietPlanner.Application.Queries.GetGoal;

using Shared.Abstractions.CQRS;

public sealed record GetGoalQuery(string UserId) : IQuery<GoalDto?>;
