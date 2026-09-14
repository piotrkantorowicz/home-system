namespace Notifications.Infrastructure.Dispatching;

using Microsoft.Extensions.Logging;
using Notifications.Application.Channels;
using Notifications.Application.Dispatching;
using Notifications.Application.Templates;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;

internal sealed partial class NotificationDispatcher(
    INotificationRepository notificationRepository,
    INotificationChannelPreferencesRepository preferencesRepository,
    INotificationTemplateRegistry templates,
    IEnumerable<INotificationChannelSender> senders,
    DapperUnitOfWork unitOfWork,
    ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    private static readonly NotificationChannel[] AllChannels =
        [NotificationChannel.Console, NotificationChannel.Email, NotificationChannel.WebSocket];

    public async Task DispatchAsync(
        NotificationType type,
        string userId,
        string locale,
        string payload,
        IReadOnlyDictionary<string, string>? placeholders,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var preferences = await preferencesRepository.GetByUserIdAsync(userId, ct);
        if (preferences is null)
        {
            preferences = NotificationChannelPreferences.CreateDefault(
                NotificationChannelPreferencesId.New(), userId, DateTime.UtcNow);
            await preferencesRepository.AddAsync(preferences, ct);
        }

        var template = templates.Resolve(type, locale);
        var title = PlaceholderRenderer.Render(template.TitleFormat, placeholders);
        var body = PlaceholderRenderer.Render(template.BodyFormat, placeholders);

        var notificationId = NotificationId.New();
        var notification = Notification.Create(
            notificationId, userId, type, title, body, payload, DateTime.UtcNow);
        await notificationRepository.AddAsync(notification, ct);

        var enabledChannels = AllChannels.Where(preferences.IsEnabled).ToList();
        var deliveries = enabledChannels
            .Select(channel => NotificationDelivery.Create(NotificationDeliveryId.New(), notificationId, channel))
            .ToList();

        foreach (var delivery in deliveries)
            await notificationRepository.AddDeliveryAsync(delivery, ct);

        await unitOfWork.CommitAsync(ct);

        // Phase 2 — actually send. Senders may be missing if a channel is enabled in
        // preferences but no sender is registered yet (e.g. Email/WebSocket land later).
        var sendersByChannel = senders.ToDictionary(s => s.Channel);

        foreach (var delivery in deliveries)
        {
            if (!sendersByChannel.TryGetValue(delivery.Channel, out var sender))
            {
                delivery.MarkSkipped(DateTime.UtcNow);
                await notificationRepository.UpdateDeliveryAsync(delivery, ct);
                continue;
            }

            try
            {
                var sendContext = new NotificationSendContext(
                    DeliveryId: delivery.Id.Value,
                    NotificationId: notification.Id.Value,
                    UserId: userId,
                    Type: type,
                    Title: title,
                    Body: body,
                    CreatedAt: notification.CreatedAt);
                var outcome = await sender.SendAsync(sendContext, ct);
                DeliveryOutcomeApplier.Apply(delivery, outcome, DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogDeliveryFailed(ex, delivery.Id, delivery.Channel);
                delivery.MarkFailed(DateTime.UtcNow, ex.Message);
            }

            await notificationRepository.UpdateDeliveryAsync(delivery, ct);
        }

        await unitOfWork.CommitAsync(ct);
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Notification delivery {DeliveryId} via {Channel} failed")]
    private partial void LogDeliveryFailed(Exception exception, NotificationDeliveryId deliveryId, NotificationChannel channel);
}
