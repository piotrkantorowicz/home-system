namespace Notifications.Application.Commands.UpdateChannelPreferences;

using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class UpdateChannelPreferencesCommandHandler(
    INotificationChannelPreferencesRepository repository,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
    : ICommandHandler<UpdateChannelPreferencesCommand>
{
    public async Task HandleAsync(UpdateChannelPreferencesCommand command, CancellationToken ct = default)
    {
        var existing = await repository.GetByUserIdAsync(command.UserId, ct);
        var now = clock.GetUtcNow().UtcDateTime;

        if (existing is null)
        {
            var prefs = NotificationChannelPreferences.CreateDefault(
                NotificationChannelPreferencesId.New(), command.UserId, now);
            prefs.Update(command.ConsoleEnabled, command.EmailEnabled, command.WebSocketEnabled, now);
            await repository.AddAsync(prefs, ct);
        }
        else
        {
            existing.Update(command.ConsoleEnabled, command.EmailEnabled, command.WebSocketEnabled, now);
            await repository.UpdateAsync(existing, ct);
        }

        await unitOfWork.CommitAsync(ct);
    }
}
