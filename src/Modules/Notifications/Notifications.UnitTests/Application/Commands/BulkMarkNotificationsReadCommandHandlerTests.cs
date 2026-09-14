namespace Notifications.UnitTests.Application.Commands;

using Notifications.Application.Commands.BulkMarkNotificationsRead;
using Notifications.Domain.Abstractions;

/// <summary>Unit tests for <c>BulkMarkNotificationsReadCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class BulkMarkNotificationsReadCommandHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();
    private readonly INotificationsUnitOfWork _uow = Substitute.For<INotificationsUnitOfWork>();
    private readonly BulkMarkNotificationsReadCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public BulkMarkNotificationsReadCommandHandlerTests()
        => _sut = new BulkMarkNotificationsReadCommandHandler(_repository, _uow);

    /// <summary>With ids: <c>Handle</c> delegates scoped bulk mark and commits.</summary>
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

    /// <summary>With empty ids: <c>Handle</c> skips repository and commit.</summary>
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

    /// <summary>With blank user id: <c>Handle</c> throws.</summary>
    [Fact]
    public async Task Handle_WithBlankUserId_Throws()
    {
        var act = () => _sut.HandleAsync(
            new BulkMarkNotificationsReadCommand(new[] { Guid.NewGuid() }, "  "),
            CancellationToken.None);

        await act.ShouldThrowAsync<ArgumentException>();
    }
}
