namespace Notifications.Application.Channels;

using Notifications.Domain.Models;

internal static class DeliveryOutcomeApplier
{
    internal static void Apply(NotificationDelivery delivery, DeliveryOutcome outcome, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        switch (outcome)
        {
            case DeliveryOutcome.Sent:
                delivery.MarkSent(utcNow);
                break;
            case DeliveryOutcome.Pending:
                delivery.RecordPendingAttempt(utcNow);
                break;
            case DeliveryOutcome.Failed:
                delivery.MarkFailed(utcNow, "sender returned Failed");
                break;
        }
    }
}
