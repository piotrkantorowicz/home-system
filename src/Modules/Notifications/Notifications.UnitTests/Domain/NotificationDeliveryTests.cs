namespace Notifications.UnitTests.Domain;

using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>NotificationDelivery</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class NotificationDeliveryTests
{
    /// <summary><c>Create</c> starts pending with zero attempts.</summary>
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

    /// <summary><c>MarkSent</c> transitions to sent and increments attempts.</summary>
    [Fact]
    public void MarkSent_TransitionsToSentAndIncrementsAttempts()
    {
        var delivery = NewPending();
        var now = TestClock.UtcNow;

        delivery.MarkSent(now);

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(now);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(1);
    }

    /// <summary><c>MarkFailed</c> transitions to failed with reason.</summary>
    [Fact]
    public void MarkFailed_TransitionsToFailedWithReason()
    {
        var delivery = NewPending();
        var now = TestClock.UtcNow;

        delivery.MarkFailed(now, "smtp 503");

        delivery.Status.ShouldBe(DeliveryStatus.Failed);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.FailureReason.ShouldBe("smtp 503");
        delivery.AttemptCount.ShouldBe(1);
    }

    /// <summary>With blank reason: <c>MarkFailed</c> throws.</summary>
    [Fact]
    public void MarkFailed_WithBlankReason_Throws()
    {
        var delivery = NewPending();

        var act = () => delivery.MarkFailed(TestClock.UtcNow, "  ");

        act.ShouldThrow<ArgumentException>();
    }

    /// <summary><c>RecordPendingAttempt</c> leaves status pending and increments attempts.</summary>
    [Fact]
    public void RecordPendingAttempt_LeavesStatusPendingAndIncrementsAttempts()
    {
        var delivery = NewPending();
        var now = TestClock.UtcNow;

        delivery.RecordPendingAttempt(now);

        delivery.Status.ShouldBe(DeliveryStatus.Pending);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(1);
        delivery.SentAt.ShouldBeNull();
    }

    /// <summary>After pending attempt: <c>MarkSent</c> transitions to sent.</summary>
    [Fact]
    public void MarkSent_AfterPendingAttempt_TransitionsToSent()
    {
        var delivery = NewPending();
        delivery.RecordPendingAttempt(TestClock.UtcNow.AddSeconds(-5));
        var ackedAt = TestClock.UtcNow;

        delivery.MarkSent(ackedAt);

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(ackedAt);
        delivery.AttemptCount.ShouldBe(2);
    }

    /// <summary>When already sent: <c>MarkSent</c> is no op.</summary>
    [Fact]
    public void MarkSent_WhenAlreadySent_IsNoOp()
    {
        var delivery = NewPending();
        var firstAck = TestClock.UtcNow;
        delivery.MarkSent(firstAck);

        delivery.MarkSent(TestClock.UtcNow.AddSeconds(10));

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.SentAt.ShouldBe(firstAck);
        delivery.AttemptCount.ShouldBe(1);
    }

    /// <summary><c>MarkSkipped</c> transitions to skipped without incrementing.</summary>
    [Fact]
    public void MarkSkipped_TransitionsToSkippedWithoutIncrementing()
    {
        var delivery = NewPending();
        var now = TestClock.UtcNow;

        delivery.MarkSkipped(now);

        delivery.Status.ShouldBe(DeliveryStatus.Skipped);
        delivery.LastAttemptAt.ShouldBe(now);
        delivery.AttemptCount.ShouldBe(0);
    }

    /// <summary><c>Retry</c> keeps the failed delivery as history and returns a fresh failed one linked to it.</summary>
    [Fact]
    public void Retry_WhenFailed_KeepsHistoryAndReturnsLinkedReplacement()
    {
        var delivery = NewPending();
        for (var i = 0; i < 5; i++)
            delivery.MarkFailed(TestClock.UtcNow, "smtp down");
        var newId = NotificationDeliveryId.New();

        var retry = delivery.Retry(newId);

        delivery.Status.ShouldBe(DeliveryStatus.Retried);
        delivery.AttemptCount.ShouldBe(5);
        delivery.FailureReason.ShouldBe("smtp down");
        retry.Id.ShouldBe(newId);
        retry.RetryOf.ShouldBe(delivery.Id);
        retry.NotificationId.ShouldBe(delivery.NotificationId);
        retry.Channel.ShouldBe(delivery.Channel);
        retry.Status.ShouldBe(DeliveryStatus.Failed);
        retry.AttemptCount.ShouldBe(0);
        retry.LastAttemptAt.ShouldBeNull();
    }

    /// <summary><c>Retry</c> rejects a delivery that is not failed, including one already retried.</summary>
    [Fact]
    public void Retry_WhenNotFailed_Throws()
    {
        var sent = NewPending();
        sent.MarkSent(TestClock.UtcNow);
        var retried = NewPending();
        retried.MarkFailed(TestClock.UtcNow, "boom");
        retried.Retry(NotificationDeliveryId.New());

        Should.Throw<DomainException>(() => sent.Retry(NotificationDeliveryId.New()));
        Should.Throw<DomainException>(() => retried.Retry(NotificationDeliveryId.New()));
        sent.Status.ShouldBe(DeliveryStatus.Sent);
    }

    private static NotificationDelivery NewPending()
        => NotificationDelivery.Create(
            NotificationDeliveryId.New(),
            NotificationId.New(),
            NotificationChannel.Console);
}
