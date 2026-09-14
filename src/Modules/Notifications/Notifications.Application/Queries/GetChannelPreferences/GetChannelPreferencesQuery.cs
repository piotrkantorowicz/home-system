namespace Notifications.Application.Queries.GetChannelPreferences;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's channel switches; every channel reads as enabled until they save once.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetChannelPreferencesQuery(string UserId) : IQuery<ChannelPreferencesDto>;
