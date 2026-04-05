namespace DietPlanner.Application.Queries.GetWaterIntake;

using Shared.Abstractions.CQRS;

public sealed record GetWaterIntakeQuery(string UserId, DateOnly Date) : IQuery<WaterIntakeListDto>;
