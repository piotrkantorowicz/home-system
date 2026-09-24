namespace DietPlanner.UnitTests.Application.Commands;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Commands.LogWaterIntake;
#pragma warning restore IDE0005
using DietPlanner.Domain.Aggregates;
using DietPlanner.Domain.Repositories;
using Microsoft.Extensions.Time.Testing;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>LogWaterIntakeCommandHandler</c>: storage, unit of work and bus boundaries are substituted with NSubstitute.</summary>
public sealed class LogWaterIntakeCommandHandlerTests
{
    private readonly IWaterIntakeRepository _repository = Substitute.For<IWaterIntakeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeTimeProvider _clock = TestClock.Create();
    private readonly LogWaterIntakeCommandHandler _sut;

    /// <summary>Builds the system under test with substituted collaborators.</summary>
    public LogWaterIntakeCommandHandlerTests()
        => _sut = new LogWaterIntakeCommandHandler(_repository, _unitOfWork, _clock);

    /// <summary>With valid command: <c>HandleAsync</c> adds entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithValidCommand_AddsEntryAndCommits()
    {
        var date = TestClock.Today;
        var command = new LogWaterIntakeCommand("user-1", date, 250, "Morning");

        var id = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<WaterIntake>(e =>
                e.UserId == "user-1" &&
                e.AmountMl == 250 &&
                e.Note == "Morning" &&
                e.Timestamp == _clock.GetUtcNow().UtcDateTime),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>With null note: <c>HandleAsync</c> adds entry and commits.</summary>
    [Fact]
    public async Task HandleAsync_WithNullNote_AddsEntryAndCommits()
    {
        var date = TestClock.Today;
        var command = new LogWaterIntakeCommand("user-1", date, 500, null);

        var id = await _sut.HandleAsync(command, TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<WaterIntake>(e => e.Note == null),
            Arg.Any<CancellationToken>());
    }
}
