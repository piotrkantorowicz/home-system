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

internal sealed class RetryDeliveryWorker(
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
                logger.LogError(ex, "Retry delivery worker tick failed");
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
            logger.LogWarning(
                "Skipping retry for delivery {DeliveryId}: parent notification {NotificationId} missing",
                delivery.Id, delivery.NotificationId);
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
            switch (outcome)
            {
                case DeliveryOutcome.Sent:
                    delivery.MarkSent(DateTime.UtcNow);
                    break;
                case DeliveryOutcome.Pending:
                    delivery.RecordPendingAttempt(DateTime.UtcNow);
                    break;
                case DeliveryOutcome.Failed:
                    delivery.MarkFailed(DateTime.UtcNow, "sender returned Failed");
                    break;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Retry of delivery {DeliveryId} via {Channel} failed (attempt {AttemptCount})",
                delivery.Id, delivery.Channel, delivery.AttemptCount + 1);
            delivery.MarkFailed(DateTime.UtcNow, ex.Message);
        }

        await repository.UpdateDeliveryAsync(delivery, ct).ConfigureAwait(false);
        await unitOfWork.CommitAsync(ct).ConfigureAwait(false);
    }
}
