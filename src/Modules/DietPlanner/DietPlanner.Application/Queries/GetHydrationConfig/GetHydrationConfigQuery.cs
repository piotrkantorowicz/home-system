namespace DietPlanner.Application.Queries.GetHydrationConfig;

using Shared.Abstractions.Cqrs;

public sealed record GetHydrationConfigQuery(string UserId) : IQuery<HydrationConfigDto?>;
