namespace DietPlanner.Application.Queries.GetHydrationConfig;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's hydration preferences; <see langword="null"/> until they have been saved once.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
public sealed record GetHydrationConfigQuery(Guid PersonId) : IQuery<HydrationConfigDto?>;
