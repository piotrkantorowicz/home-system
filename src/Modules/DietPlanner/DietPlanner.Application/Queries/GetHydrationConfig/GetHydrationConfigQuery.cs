namespace DietPlanner.Application.Queries.GetHydrationConfig;

using Shared.Abstractions.CQRS;

public sealed record GetHydrationConfigQuery(string UserId) : IQuery<HydrationConfigDto?>;
