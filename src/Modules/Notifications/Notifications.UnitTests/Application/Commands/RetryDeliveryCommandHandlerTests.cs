namespace Notifications.UnitTests.Application.Commands;

using Notifications.Application.Commands.RetryDelivery;
using Notifications.Domain.Abstractions;
using Notifications.Domain.Models;
using Notifications.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>RetryDeliveryCommandHandler</c>: storage and unit of work are substituted with NSubstitute.</summary>
public sealed class RetryDeliveryCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationsUnitOfWork _uow = Substitute.For<INotificationsUnitOfWork>();
    private readonly RetryDeliveryCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public RetryDeliveryCommandHandlerTests()
        => _sut = new RetryDeliveryCommandHandler(_repository, _uow);

    /// <summary>For a dead-lettered delivery: <c>Handle</c> resets its attempts and commits.</summary>
    [Fact]
    public async Task Handle_WhenFailed_RequeuesAndCommits()
    {
        var delivery = NotificationDelivery.Create(
            NotificationDeliveryId.New(), NotificationId.New(), NotificationChannel.Console);
        for (var i = 0; i < 5; i++)
            delivery.MarkFailed(TestClock.UtcNow, "boom");
        _repository.GetDeliveryAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(delivery);

        await _sut.HandleAsync(new RetryDeliveryCommand(delivery.Id.Value), TestContext.Current.CancellationToken);

        delivery.AttemptCount.ShouldBe(0);
        await _repository.Received(1).UpdateDeliveryAsync(delivery, Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>For an unknown delivery: <c>Handle</c> throws <see cref="NotFoundException"/> and commits nothing.</summary>
    [Fact]
    public async Task Handle_WhenMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() =>
            _sut.HandleAsync(new RetryDeliveryCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
