namespace Notifications.UnitTests.Application.Commands;

using Notifications.Application.Commands.AckNotificationDelivery;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>AckNotificationDeliveryCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class AckNotificationDeliveryCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationsUnitOfWork _uow = Substitute.For<INotificationsUnitOfWork>();
    private readonly AckNotificationDeliveryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public AckNotificationDeliveryCommandHandlerTests()
        => _sut = new AckNotificationDeliveryCommandHandler(_repository, _uow);

    /// <summary>When owned by user: <c>Handle</c> marks sent and commits.</summary>
    [Fact]
    public async Task Handle_WhenOwnedByUser_MarksSentAndCommits()
    {
        var userId = "user-1";
        var notificationId = NotificationId.New();
        var deliveryId = NotificationDeliveryId.New();
        var delivery = NotificationDelivery.Create(deliveryId, notificationId, NotificationChannel.WebSocket);
        delivery.RecordPendingAttempt(DateTime.UtcNow.AddSeconds(-5));

        var notification = Notification.Create(
            notificationId, userId, NotificationType.WaterReminder,
            "title", "body", "{}", DateTime.UtcNow);

        _repository.GetDeliveryAsync(deliveryId, Arg.Any<CancellationToken>())
            .Returns(delivery);
        _repository.GetByIdAsync(notificationId, Arg.Any<CancellationToken>())
            .Returns(notification);

        await _sut.HandleAsync(new AckNotificationDeliveryCommand(deliveryId.Value, userId), CancellationToken.None);

        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        await _repository.Received(1).UpdateDeliveryAsync(delivery, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When foreign owner: <c>Handle</c> throws and does not mutate.</summary>
    [Fact]
    public async Task Handle_WhenForeignOwner_ThrowsAndDoesNotMutate()
    {
        var notificationId = NotificationId.New();
        var deliveryId = NotificationDeliveryId.New();
        var delivery = NotificationDelivery.Create(deliveryId, notificationId, NotificationChannel.WebSocket);
        var notification = Notification.Create(
            notificationId, "owner", NotificationType.WaterReminder,
            "t", "b", "{}", DateTime.UtcNow);

        _repository.GetDeliveryAsync(deliveryId, Arg.Any<CancellationToken>()).Returns(delivery);
        _repository.GetByIdAsync(notificationId, Arg.Any<CancellationToken>()).Returns(notification);

        var act = () => _sut.HandleAsync(
            new AckNotificationDeliveryCommand(deliveryId.Value, "intruder"),
            CancellationToken.None);

        await act.ShouldThrowAsync<NotFoundException>();
        await _repository.DidNotReceive().UpdateDeliveryAsync(Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When already sent: <c>Handle</c> is no op.</summary>
    [Fact]
    public async Task Handle_WhenAlreadySent_IsNoOp()
    {
        var userId = "user-1";
        var notificationId = NotificationId.New();
        var deliveryId = NotificationDeliveryId.New();
        var delivery = NotificationDelivery.Create(deliveryId, notificationId, NotificationChannel.WebSocket);
        delivery.MarkSent(DateTime.UtcNow);

        var notification = Notification.Create(
            notificationId, userId, NotificationType.WaterReminder,
            "t", "b", "{}", DateTime.UtcNow);

        _repository.GetDeliveryAsync(deliveryId, Arg.Any<CancellationToken>()).Returns(delivery);
        _repository.GetByIdAsync(notificationId, Arg.Any<CancellationToken>()).Returns(notification);

        await _sut.HandleAsync(new AckNotificationDeliveryCommand(deliveryId.Value, userId), CancellationToken.None);

        await _repository.DidNotReceive().UpdateDeliveryAsync(Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>When delivery missing: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WhenDeliveryMissing_Throws()
    {
        _repository.GetDeliveryAsync(Arg.Any<NotificationDeliveryId>(), Arg.Any<CancellationToken>())
            .Returns((NotificationDelivery?)null);

        var act = () => _sut.HandleAsync(
            new AckNotificationDeliveryCommand(Guid.NewGuid(), "user-1"),
            CancellationToken.None);

        await act.ShouldThrowAsync<NotFoundException>();
    }
}
