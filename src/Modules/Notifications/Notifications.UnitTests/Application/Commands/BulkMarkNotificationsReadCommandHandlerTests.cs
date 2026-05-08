namespace Notifications.UnitTests.Application.Commands;

using Notifications.Application.Commands.BulkMarkNotificationsRead;
using Notifications.Domain.Abstractions;

public sealed class BulkMarkNotificationsReadCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationsUnitOfWork _uow = Substitute.For<INotificationsUnitOfWork>();
    private readonly BulkMarkNotificationsReadCommandHandler _sut;

    public BulkMarkNotificationsReadCommandHandlerTests()
        => _sut = new BulkMarkNotificationsReadCommandHandler(_repository, _uow);

    [Fact]
    public async Task Handle_WithIds_DelegatesScopedBulkMarkAndCommits()
    {
        var userId = "user-1";
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _repository.BulkMarkReadAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>())
            .Returns(3);

        await _sut.HandleAsync(new BulkMarkNotificationsReadCommand(ids, userId), CancellationToken.None);

        await _repository.Received(1).BulkMarkReadAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(c => c.Count == 3),
            userId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyIds_SkipsRepositoryAndCommit()
    {
        await _sut.HandleAsync(
            new BulkMarkNotificationsReadCommand(Array.Empty<Guid>(), "user-1"),
            CancellationToken.None);

        await _repository.DidNotReceive().BulkMarkReadAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBlankUserId_Throws()
    {
        var act = () => _sut.HandleAsync(
            new BulkMarkNotificationsReadCommand(new[] { Guid.NewGuid() }, "  "),
            CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
