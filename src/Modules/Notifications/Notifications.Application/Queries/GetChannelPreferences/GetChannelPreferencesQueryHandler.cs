namespace Notifications.Application.Queries.GetChannelPreferences;

using Notifications.Domain.Abstractions;
using Shared.Abstractions.Cqrs;

internal sealed class GetChannelPreferencesQueryHandler(
    INotificationChannelPreferencesRepository repository)
    : IQueryHandler<GetChannelPreferencesQuery, ChannelPreferencesDto>
{
    public async Task<ChannelPreferencesDto> HandleAsync(GetChannelPreferencesQuery query, CancellationToken ct = default)
    {
        var prefs = await repository.GetByUserIdAsync(query.UserId, ct);
        // Defaults (all enabled) when user has not set preferences yet — keeps the
        // GET endpoint simple (no 404 special case) and matches the lazy-create
        // behaviour of the dispatcher.
        return prefs is null
            ? new ChannelPreferencesDto(true, true, true)
            : new ChannelPreferencesDto(prefs.ConsoleEnabled, prefs.EmailEnabled, prefs.WebSocketEnabled);
    }
}
