namespace DietPlanner.Application.Queries.GetWaterIntake;

using Shared.Abstractions.Cqrs;

public sealed record GetWaterIntakeQuery(string UserId, DateOnly Date) : IQuery<WaterIntakeListDto>;
