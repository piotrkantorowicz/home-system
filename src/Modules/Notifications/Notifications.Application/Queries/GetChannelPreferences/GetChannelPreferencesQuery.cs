namespace Notifications.Application.Queries.GetChannelPreferences;

using Shared.Abstractions.Cqrs;

public sealed record GetChannelPreferencesQuery(string UserId) : IQuery<ChannelPreferencesDto>;
