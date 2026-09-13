namespace Notifications.Infrastructure.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Notifications.Application.Channels;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Notifications.Infrastructure.Persistence;

internal sealed partial class RetryDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RetryDeliveryWorkerOptions> options,
    ILogger<RetryDeliveryWorker> logger) : BackgroundService
{
    private readonly RetryDeliveryWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogTickFailed(ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var repository = sp.GetRequiredService<INotificationRepository>();
        var unitOfWork = sp.GetRequiredService<DapperUnitOfWork>();
        var senders = sp.GetRequiredService<IEnumerable<INotificationChannelSender>>()
            .ToDictionary(s => s.Channel);

        var due = await repository
            .GetFailedDeliveriesForRetryAsync(_options.BatchSize, _options.MaxAttempts, ct)
            .ConfigureAwait(false);

        if (due.Count == 0) return;

        foreach (var delivery in due)
        {
            await RetryAsync(delivery, repository, unitOfWork, senders, ct).ConfigureAwait(false);
        }
    }

    private async Task RetryAsync(
        NotificationDelivery delivery,
        INotificationRepository repository,
        DapperUnitOfWork unitOfWork,
        IDictionary<NotificationChannel, INotificationChannelSender> senders,
        CancellationToken ct)
    {
        var notification = await repository.GetByIdAsync(delivery.NotificationId, ct).ConfigureAwait(false);
        if (notification is null)
        {
            LogParentMissing(delivery.Id, delivery.NotificationId);
            return;
        }

        if (!senders.TryGetValue(delivery.Channel, out var sender))
        {
            delivery.MarkSkipped(DateTime.UtcNow);
            await repository.UpdateDeliveryAsync(delivery, ct).ConfigureAwait(false);
            await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
            return;
        }

        try
        {
            var sendContext = new NotificationSendContext(
                DeliveryId: delivery.Id.Value,
                NotificationId: notification.Id.Value,
                UserId: notification.UserId,
                Type: notification.Type,
                Title: notification.Title,
                Body: notification.Body,
                CreatedAt: notification.CreatedAt);
            var outcome = await sender.SendAsync(sendContext, ct).ConfigureAwait(false);
            DeliveryOutcomeApplier.Apply(delivery, outcome, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogRetryFailed(ex, delivery.Id, delivery.Channel, delivery.AttemptCount + 1);
            delivery.MarkFailed(DateTime.UtcNow, ex.Message);
        }

        await repository.UpdateDeliveryAsync(delivery, ct).ConfigureAwait(false);
        await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Retry delivery worker tick failed")]
    private partial void LogTickFailed(Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Skipping retry for delivery {DeliveryId}: parent notification {NotificationId} missing")]
    private partial void LogParentMissing(NotificationDeliveryId deliveryId, NotificationId notificationId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Retry of delivery {DeliveryId} via {Channel} failed (attempt {AttemptCount})")]
    private partial void LogRetryFailed(
        Exception exception, NotificationDeliveryId deliveryId, NotificationChannel channel, int attemptCount);
}
