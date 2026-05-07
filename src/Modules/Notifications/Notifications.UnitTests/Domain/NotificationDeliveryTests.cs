namespace Notifications.UnitTests.Domain;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;

public sealed class NotificationDeliveryTests
{
    [Fact]
    public void Create_StartsPendingWithZeroAttempts()
    {
        var delivery = NotificationDelivery.Create(
            NotificationDeliveryId.New(),
            NotificationId.New(),
            NotificationChannel.Console);

        delivery.Status.ShouldBe(DeliveryStatus.Pending);
        delivery.AttemptCount.ShouldBe(0);
        delivery.SentAt.ShouldBeNull();
        delivery.LastAttemptAt.ShouldBeNull();
        delivery.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void MarkSent_TransitionsToSentAndIncrementsAttempts()
    {
        var delivery = NewPending();
        var now = DateTime.UtcNow;

        delivery.MarkSent(now);

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(now);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void MarkFailed_TransitionsToFailedWithReason()
    {
        var delivery = NewPending();
        var now = DateTime.UtcNow;

        delivery.MarkFailed(now, "smtp 503");

        delivery.Status.ShouldBe(DeliveryStatus.Failed);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.FailureReason.ShouldBe("smtp 503");
        delivery.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void MarkFailed_WithBlankReason_Throws()
    {
        var delivery = NewPending();

        var act = () => delivery.MarkFailed(DateTime.UtcNow, "  ");

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void RecordPendingAttempt_LeavesStatusPendingAndIncrementsAttempts()
    {
        var delivery = NewPending();
        var now = DateTime.UtcNow;

        delivery.RecordPendingAttempt(now);

        delivery.Status.ShouldBe(DeliveryStatus.Pending);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(1);
        delivery.SentAt.ShouldBeNull();
    }

    [Fact]
    public void MarkSent_AfterPendingAttempt_TransitionsToSent()
    {
        var delivery = NewPending();
        delivery.RecordPendingAttempt(DateTime.UtcNow.AddSeconds(-5));
        var ackedAt = DateTime.UtcNow;

        delivery.MarkSent(ackedAt);

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(ackedAt);
        delivery.AttemptCount.ShouldBe(2);
    }

    [Fact]
    public void MarkSent_WhenAlreadySent_IsNoOp()
    {
        var delivery = NewPending();
        var firstAck = DateTime.UtcNow;
        delivery.MarkSent(firstAck);

        delivery.MarkSent(DateTime.UtcNow.AddSeconds(10));

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(firstAck);
        delivery.AttemptCount.ShouldBe(1);
    }

    [Fact]
    public void MarkSkipped_TransitionsToSkippedWithoutIncrementing()
    {
        var delivery = NewPending();
        var now = DateTime.UtcNow;

        delivery.MarkSkipped(now);

        delivery.Status.ShouldBe(DeliveryStatus.Skipped);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(0);
    }

    private static NotificationDelivery NewPending()
        => NotificationDelivery.Create(
            NotificationDeliveryId.New(),
            NotificationId.New(),
            NotificationChannel.Console);
}
